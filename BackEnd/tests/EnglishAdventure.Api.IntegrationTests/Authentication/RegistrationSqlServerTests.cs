using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EnglishAdventure.Api.IntegrationTests.Authentication;

/// <summary>
/// Parent self-registration with email confirmation (S1.4, PD-009) through the real middleware and SQL Server.
/// </summary>
public sealed partial class RegistrationSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string RegisterRoute = "/api/v1/auth/register";
    private const string ConfirmRoute = "/api/v1/auth/confirm-email";
    private const string ResendRoute = "/api/v1/auth/resend-confirmation";
    private const string LoginRoute = "/api/v1/auth/login";
    private const string Password = "Correct-horse-1";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public RegistrationSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient Client => _fixture.Factory.CreateClient();

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.test";

    [SqlServerFact]
    public async Task Registering_creates_an_unconfirmed_parent_and_emails_a_link()
    {
        await EnsureParentRoleAsync();
        var email = NewEmail();

        var response = await RegisterAsync(email, language: "en");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());

        var user = await _fixture.WithDbContextAsync(db => db.Users
            .Include(candidate => candidate.Roles)
            .SingleAsync(candidate => candidate.NormalizedEmail == email.ToUpperInvariant()));
        Assert.Equal(UserType.Parent, user.UserType);
        Assert.False(user.IsEmailConfirmed);
        Assert.Equal(Consent.CurrentVersion, user.ConsentVersion);
        Assert.NotNull(user.ConsentAcceptedAtUtc);
        Assert.Single(user.Roles);

        var message = Assert.Single(_fixture.Emails.SentTo(email));
        Assert.Equal("Confirm your English Adventure account", message.Subject);
        var token = TokenFrom(message.TextBody);
        Assert.Contains("http://localhost:5173/confirm-email?token=", message.TextBody);

        var storedHashes = await _fixture.WithDbContextAsync(db => db.EmailConfirmationTokens
            .Where(candidate => candidate.UserId == user.Id)
            .Select(candidate => candidate.TokenHash)
            .ToListAsync());
        Assert.DoesNotContain(token, Assert.Single(storedHashes));
    }

    [SqlServerFact]
    public async Task An_unconfirmed_parent_cannot_sign_in_and_is_told_why()
    {
        await EnsureParentRoleAsync();
        var email = NewEmail();
        await RegisterAsync(email);

        var wrongPassword = await Client.PostAsJsonAsync(LoginRoute, new { email, Password = "Wrong-password-1" });
        var rightPassword = await Client.PostAsJsonAsync(LoginRoute, new { email, Password });

        // The reason is only given with the right password, so it does not reveal that the email is registered.
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rightPassword.StatusCode);
        var problem = await rightPassword.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("email-not-confirmed", problem.GetProperty("code").GetString());
    }

    [SqlServerFact]
    public async Task The_link_confirms_the_email_and_then_the_parent_can_sign_in()
    {
        await EnsureParentRoleAsync();
        var email = NewEmail();
        await RegisterAsync(email);
        var token = TokenFrom(Assert.Single(_fixture.Emails.SentTo(email)).TextBody);

        var confirm = await Client.PostAsJsonAsync(ConfirmRoute, new { token });
        var confirmAgain = await Client.PostAsJsonAsync(ConfirmRoute, new { token });
        var login = await Client.PostAsJsonAsync(LoginRoute, new { email, Password });

        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        // Opening the link again (a second click, a reload) still reports success.
        Assert.Equal(HttpStatusCode.NoContent, confirmAgain.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [SqlServerFact]
    public async Task An_expired_or_unknown_link_is_rejected()
    {
        await EnsureParentRoleAsync();
        var email = NewEmail();
        await RegisterAsync(email);
        var token = TokenFrom(Assert.Single(_fixture.Emails.SentTo(email)).TextBody);
        await _fixture.WithDbContextAsync(db => db.EmailConfirmationTokens
            .Where(candidate => candidate.TokenHash != string.Empty)
            .Where(candidate => db.Users.Any(user => user.Id == candidate.UserId && user.NormalizedEmail == email.ToUpperInvariant()))
            .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1))));

        var expired = await Client.PostAsJsonAsync(ConfirmRoute, new { token });
        var unknown = await Client.PostAsJsonAsync(ConfirmRoute, new { token = "never-issued" });

        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        var problem = await expired.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("Token", out _));
    }

    [SqlServerFact]
    public async Task Registering_a_taken_email_answers_the_same_and_only_notifies_the_owner()
    {
        await EnsureParentRoleAsync();
        var owner = await _fixture.CreateUserAsync(UserType.Parent, roles: SystemRoles.Parent);

        var response = await RegisterAsync(owner.Email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        var message = Assert.Single(_fixture.Emails.SentTo(owner.Email));
        Assert.Equal("Bạn đã có tài khoản English Adventure", message.Subject);
        Assert.Contains("http://localhost:5173/login", message.TextBody);

        // The owner's password was not replaced by the one sent with the second registration.
        var login = await Client.PostAsJsonAsync(LoginRoute, new { owner.Email, owner.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [SqlServerFact]
    public async Task A_new_link_voids_the_previous_one()
    {
        await EnsureParentRoleAsync();
        var email = NewEmail();
        await RegisterAsync(email);
        var firstToken = TokenFrom(Assert.Single(_fixture.Emails.SentTo(email)).TextBody);

        var resend = await Client.PostAsJsonAsync(ResendRoute, new { email });
        var secondToken = TokenFrom(_fixture.Emails.SentTo(email)[1].TextBody);

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync(ConfirmRoute, new { token = firstToken })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsJsonAsync(ConfirmRoute, new { token = secondToken })).StatusCode);
    }

    [SqlServerFact]
    public async Task Resending_says_nothing_about_unknown_or_confirmed_emails()
    {
        var confirmed = await _fixture.CreateUserAsync(UserType.Parent);
        var unknown = NewEmail();

        var forConfirmed = await Client.PostAsJsonAsync(ResendRoute, new { confirmed.Email });
        var forUnknown = await Client.PostAsJsonAsync(ResendRoute, new { Email = unknown });

        Assert.Equal(HttpStatusCode.Accepted, forConfirmed.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, forUnknown.StatusCode);
        Assert.Empty(_fixture.Emails.SentTo(confirmed.Email));
        Assert.Empty(_fixture.Emails.SentTo(unknown));
    }

    [SqlServerFact]
    public async Task Registering_needs_accepted_terms_and_a_strong_password()
    {
        var withoutTerms = await Client.PostAsJsonAsync(
            RegisterRoute,
            new { Email = NewEmail(), Password, AcceptTerms = false });
        var weakPassword = await Client.PostAsJsonAsync(
            RegisterRoute,
            new { Email = NewEmail(), Password = "short", AcceptTerms = true });

        Assert.Equal(HttpStatusCode.BadRequest, withoutTerms.StatusCode);
        Assert.True((await withoutTerms.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("AcceptTerms", out _));
        Assert.Equal(HttpStatusCode.BadRequest, weakPassword.StatusCode);
        Assert.True((await weakPassword.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("Password", out _));
    }

    [SqlServerFact]
    public async Task A_deactivated_user_signing_in_gets_its_own_code()
    {
        var user = await _fixture.CreateUserAsync(UserType.Parent, isActive: false);

        var response = await Client.PostAsJsonAsync(LoginRoute, new { user.Email, user.Password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("user-inactive", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [SqlServerFact]
    public async Task Too_many_registrations_from_one_address_are_refused()
    {
        await EnsureParentRoleAsync();
        using var limited = _fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["RateLimiting:RegistrationPermitLimit"] = "2" })));
        var client = limited.CreateClient();

        var responses = new List<HttpResponseMessage>();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            responses.Add(await client.PostAsJsonAsync(RegisterRoute, new { Email = NewEmail(), Password, AcceptTerms = true }));
        }

        Assert.Equal([HttpStatusCode.Accepted, HttpStatusCode.Accepted, HttpStatusCode.TooManyRequests], responses.Select(response => response.StatusCode));
        var problem = await responses[2].Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
        Assert.True(responses[2].Headers.Contains("Retry-After"));
    }

    private Task<HttpResponseMessage> RegisterAsync(string email, string? language = null) =>
        Client.PostAsJsonAsync(RegisterRoute, new { email, Password, AcceptTerms = true, language });

    private Task EnsureParentRoleAsync() => _fixture.GrantPermissionsAsync(SystemRoles.Parent);

    private static string TokenFrom(string emailBody)
    {
        var match = ConfirmationLink().Match(emailBody);
        Assert.True(match.Success, "The email contains no confirmation link.");
        return Uri.UnescapeDataString(match.Groups["token"].Value);
    }

    [GeneratedRegex(@"/confirm-email\?token=(?<token>\S+)")]
    private static partial Regex ConfirmationLink();
}
