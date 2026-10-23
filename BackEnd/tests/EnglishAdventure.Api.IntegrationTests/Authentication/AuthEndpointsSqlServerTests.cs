using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Domain.Users;
using EnglishAdventure.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EnglishAdventure.Api.IntegrationTests.Authentication;

/// <summary>
/// End-to-end auth flows through the real middleware, Identity stores and refresh-token persistence on SQL Server.
/// </summary>
public sealed class AuthEndpointsSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LoginRoute = "/api/v1/auth/login";
    private const string RefreshRoute = "/api/v1/auth/refresh";
    private const string LogoutRoute = "/api/v1/auth/logout";
    private const string MeRoute = "/api/v1/auth/me";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public AuthEndpointsSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient Client => _fixture.Factory.CreateClient();

    [SqlServerFact]
    public async Task Login_returns_tokens_with_subject_account_type_and_roles_but_no_email()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent, roles: "Parent");

        var response = await Client.PostAsJsonAsync(LoginRoute, new { account.Email, account.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        var tokens = await ReadTokensAsync(response);
        Assert.Equal("Bearer", tokens.TokenType);

        var accessToken = new JsonWebToken(tokens.AccessToken);
        Assert.Equal(account.Id.ToString(), accessToken.Subject);
        Assert.Equal("Parent", accessToken.GetClaim(AuthClaimTypes.AccountType).Value);
        Assert.Equal("Parent", accessToken.GetClaim(AuthClaimTypes.Role).Value);
        Assert.False(string.IsNullOrEmpty(accessToken.Id));
        Assert.False(accessToken.TryGetClaim("email", out _));
    }

    [SqlServerFact]
    public async Task Refresh_tokens_are_stored_only_as_hashes()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Child);
        var tokens = await LoginAsync(account);

        var storedHashes = await _fixture.WithDbContextAsync(db => db.RefreshTokens
            .Where(token => token.UserId == account.Id)
            .Select(token => token.TokenHash)
            .ToListAsync());

        var storedHash = Assert.Single(storedHashes);
        Assert.NotEqual(tokens.RefreshToken, storedHash);
        Assert.DoesNotContain(tokens.RefreshToken, storedHash);
    }

    [SqlServerFact]
    public async Task Login_failures_for_unknown_email_and_wrong_password_are_indistinguishable()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);

        var wrongPassword = await Client.PostAsJsonAsync(LoginRoute, new { account.Email, Password = "Wrong-password-1" });
        var unknownEmail = await Client.PostAsJsonAsync(LoginRoute, new { Email = "nobody@example.test", Password = "Wrong-password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);

        var wrongPasswordBody = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var unknownEmailBody = await unknownEmail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(wrongPasswordBody.GetProperty("title").GetString(), unknownEmailBody.GetProperty("title").GetString());
        Assert.Equal(wrongPasswordBody.GetProperty("detail").GetString(), unknownEmailBody.GetProperty("detail").GetString());
    }

    [SqlServerFact]
    public async Task Login_is_rejected_after_lockout_even_with_the_correct_password()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failed = await Client.PostAsJsonAsync(LoginRoute, new { account.Email, Password = "Wrong-password-1" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var response = await Client.PostAsJsonAsync(LoginRoute, new { account.Email, account.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var lockoutEnd = await _fixture.WithDbContextAsync(db => db.Users
            .Where(user => user.Id == account.Id)
            .Select(user => user.LockoutEnd)
            .SingleAsync());
        Assert.NotNull(lockoutEnd);
        Assert.True(lockoutEnd > DateTimeOffset.UtcNow);
    }

    [SqlServerFact]
    public async Task Inactive_account_gets_403_only_with_the_correct_password()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Child, isActive: false);

        var correctPassword = await Client.PostAsJsonAsync(LoginRoute, new { account.Email, account.Password });
        var wrongPassword = await Client.PostAsJsonAsync(LoginRoute, new { account.Email, Password = "Wrong-password-1" });

        Assert.Equal(HttpStatusCode.Forbidden, correctPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
    }

    [SqlServerFact]
    public async Task Me_returns_the_profile_for_a_logged_in_user()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Admin, roles: "Admin");
        var tokens = await LoginAsync(account);

        using var request = new HttpRequestMessage(HttpMethod.Get, MeRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(account.Id, body.GetProperty("userId").GetGuid());
        Assert.Equal(account.Email, body.GetProperty("email").GetString());
        Assert.Equal("Admin", body.GetProperty("accountType").GetString());
        Assert.Equal("Admin", body.GetProperty("roles")[0].GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));
    }

    [SqlServerFact]
    public async Task Refresh_rotates_the_token_pair()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);
        var login = await LoginAsync(account);

        var response = await RefreshAsync(login.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = await ReadTokensAsync(response);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        Assert.NotEqual(login.AccessToken, rotated.AccessToken);
        Assert.Equal(account.Id.ToString(), new JsonWebToken(rotated.AccessToken).Subject);

        var tokens = await LoadTokensAsync(account.Id);
        Assert.Equal(2, tokens.Count);
        Assert.Single(tokens, token => token.FamilyId == tokens[0].FamilyId && token.RevokedAtUtc is null);
        var consumed = Assert.Single(tokens, token => token.RevokedAtUtc is not null);
        Assert.Equal(RefreshTokenRevocationReason.Rotated, consumed.RevocationReason);
        Assert.NotNull(consumed.ReplacedByTokenId);
    }

    [SqlServerFact]
    public async Task Reusing_a_rotated_token_is_rejected_and_revokes_the_whole_family()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);
        var login = await LoginAsync(account);
        var rotated = await ReadTokensAsync(await RefreshAsync(login.RefreshToken));

        var replay = await RefreshAsync(login.RefreshToken);
        var afterReplay = await RefreshAsync(rotated.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);

        var tokens = await LoadTokensAsync(account.Id);
        Assert.All(tokens, token => Assert.NotNull(token.RevokedAtUtc));
        Assert.Contains(tokens, token => token.RevocationReason == RefreshTokenRevocationReason.ReuseDetected);
    }

    [SqlServerFact]
    public async Task Reuse_detection_does_not_affect_other_sessions_of_the_same_user()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);
        var compromised = await LoginAsync(account);
        var otherDevice = await LoginAsync(account);
        await RefreshAsync(compromised.RefreshToken);

        await RefreshAsync(compromised.RefreshToken);
        var otherDeviceRefresh = await RefreshAsync(otherDevice.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, otherDeviceRefresh.StatusCode);
    }

    [SqlServerFact]
    public async Task Concurrent_refreshes_with_the_same_token_succeed_at_most_once()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);
        var login = await LoginAsync(account);
        var client = Client;

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            client.PostAsJsonAsync(RefreshRoute, new { login.RefreshToken })));

        var winners = responses.Where(response => response.StatusCode == HttpStatusCode.OK).ToList();
        Assert.Single(winners);
        Assert.All(
            responses.Where(response => response.StatusCode != HttpStatusCode.OK),
            response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));

        // The losing requests count as reuse, so the winner's new token is revoked as well.
        var winnerTokens = await ReadTokensAsync(winners[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(winnerTokens.RefreshToken)).StatusCode);

        var tokens = await LoadTokensAsync(account.Id);
        Assert.Equal(1, tokens.Count(token => token.RevocationReason == RefreshTokenRevocationReason.Rotated));
        Assert.All(tokens, token => Assert.NotNull(token.RevokedAtUtc));
    }

    [SqlServerFact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Child);
        var login = await LoginAsync(account);
        await _fixture.WithDbContextAsync(db => db.RefreshTokens
            .Where(token => token.UserId == account.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1))));

        var response = await RefreshAsync(login.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SqlServerFact]
    public async Task Unknown_refresh_token_is_rejected()
    {
        var response = await RefreshAsync("this-token-was-never-issued");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SqlServerFact]
    public async Task Logout_revokes_the_session_and_is_idempotent()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);
        var login = await LoginAsync(account);

        var first = await Client.PostAsJsonAsync(LogoutRoute, new { login.RefreshToken });
        var second = await Client.PostAsJsonAsync(LogoutRoute, new { login.RefreshToken });
        var unknown = await Client.PostAsJsonAsync(LogoutRoute, new { RefreshToken = "never-issued" });
        var refreshAfterLogout = await RefreshAsync(login.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);

        var token = Assert.Single(await LoadTokensAsync(account.Id));
        Assert.Equal(RefreshTokenRevocationReason.Revoked, token.RevocationReason);
    }

    [SqlServerFact]
    public async Task Refresh_after_the_account_is_deactivated_is_rejected_and_ends_the_session()
    {
        var account = await _fixture.CreateAccountAsync(AccountType.Parent);
        var login = await LoginAsync(account);
        await _fixture.WithDbContextAsync(db => db.Users
            .Where(user => user.Id == account.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.IsActive, false)));

        var response = await RefreshAsync(login.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.All(await LoadTokensAsync(account.Id), token => Assert.NotNull(token.RevokedAtUtc));
    }

    private async Task<TokensBody> LoginAsync(TestAccount account)
    {
        var response = await Client.PostAsJsonAsync(LoginRoute, new { account.Email, account.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadTokensAsync(response);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        Client.PostAsJsonAsync(RefreshRoute, new { RefreshToken = refreshToken });

    private Task<List<RefreshToken>> LoadTokensAsync(Guid userId) =>
        _fixture.WithDbContextAsync(db => db.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == userId)
            .OrderBy(token => token.CreatedAtUtc)
            .ToListAsync());

    private static async Task<TokensBody> ReadTokensAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<TokensBody>();
        Assert.NotNull(body);
        return body;
    }

    private sealed record TokensBody(
        string AccessToken,
        string TokenType,
        DateTime AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc);
}
