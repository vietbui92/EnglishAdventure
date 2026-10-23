using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Domain.Users;
using EnglishAdventure.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EnglishAdventure.Api.IntegrationTests.Authentication;

/// <summary>
/// JWT bearer middleware and the /me endpoint, without a database: the identity store is replaced by a fake.
/// </summary>
public sealed class AccessTokenAuthenticationTests : IClassFixture<EnglishAdventureWebApplicationFactory>
{
    private const string MeRoute = "/api/v1/auth/me";

    private static readonly AuthenticatedUser KnownUser =
        new(Guid.NewGuid(), "parent@example.test", AccountType.Parent, IsActive: true, IsLockedOut: false);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly JwtOptions _jwtOptions;

    public AccessTokenAuthenticationTests(EnglishAdventureWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityUserService>();
            services.AddSingleton<IIdentityUserService>(new SingleUserIdentityService(KnownUser, "Parent"));
        }));
        _jwtOptions = _factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
    }

    [Fact]
    public async Task Me_returns_401_problem_details_without_a_token()
    {
        var response = await _factory.CreateClient().GetAsync(MeRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Me_returns_the_current_user_for_an_issued_access_token()
    {
        var issuer = _factory.Services.GetRequiredService<IAccessTokenIssuer>();
        var (accessToken, _) = issuer.Issue(KnownUser.Id, KnownUser.AccountType, new[] { "Parent" });

        var response = await SendMeAsync(accessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(KnownUser.Id, body.GetProperty("userId").GetGuid());
        Assert.Equal(KnownUser.Email, body.GetProperty("email").GetString());
        Assert.Equal("Parent", body.GetProperty("accountType").GetString());
        Assert.Equal("Parent", body.GetProperty("roles")[0].GetString());
    }

    [Fact]
    public void Issued_access_token_contains_only_the_expected_claims()
    {
        var issuer = _factory.Services.GetRequiredService<IAccessTokenIssuer>();
        var (accessToken, expiresAtUtc) = issuer.Issue(KnownUser.Id, AccountType.Child, Array.Empty<string>());

        var token = new JsonWebToken(accessToken);

        Assert.Equal(KnownUser.Id.ToString(), token.Subject);
        Assert.Equal("Child", token.GetClaim(AuthClaimTypes.AccountType).Value);
        Assert.False(string.IsNullOrEmpty(token.Id));
        Assert.Equal(_jwtOptions.Issuer, token.Issuer);
        Assert.Equal(_jwtOptions.Audience, Assert.Single(token.Audiences));
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Alg);
        Assert.False(token.TryGetClaim("email", out _));
        Assert.False(token.TryGetClaim(AuthClaimTypes.Role, out _));
        Assert.InRange(
            (expiresAtUtc - DateTime.UtcNow).TotalMinutes,
            _jwtOptions.AccessTokenLifetimeMinutes - 1,
            _jwtOptions.AccessTokenLifetimeMinutes);
    }

    [Fact]
    public async Task Me_rejects_an_expired_access_token()
    {
        var now = DateTime.UtcNow;
        var token = CreateToken(_jwtOptions.CreateSigningKey(), _jwtOptions.Issuer, _jwtOptions.Audience,
            issuedAt: now.AddMinutes(-30), expires: now.AddMinutes(-5));

        var response = await SendMeAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_rejects_a_token_signed_with_a_different_key()
    {
        var otherKey = new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        var token = CreateToken(otherKey, _jwtOptions.Issuer, _jwtOptions.Audience);

        var response = await SendMeAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("wrong-issuer", null)]
    [InlineData(null, "wrong-audience")]
    public async Task Me_rejects_a_token_with_the_wrong_issuer_or_audience(string? issuer, string? audience)
    {
        var token = CreateToken(_jwtOptions.CreateSigningKey(), issuer ?? _jwtOptions.Issuer, audience ?? _jwtOptions.Audience);

        var response = await SendMeAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_rejects_an_unsigned_token()
    {
        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            Subject = new ClaimsIdentity(new[] { new Claim(AuthClaimTypes.Subject, KnownUser.Id.ToString()) }),
            Expires = DateTime.UtcNow.AddMinutes(5)
        });

        var response = await SendMeAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_rejects_a_malformed_token()
    {
        var response = await SendMeAsync("not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_invalid_input_returns_400_validation_problem()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out _));
    }

    private async Task<HttpResponseMessage> SendMeAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MeRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _factory.CreateClient().SendAsync(request);
    }

    private static string CreateToken(
        SecurityKey signingKey,
        string issuer,
        string audience,
        DateTime? issuedAt = null,
        DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };

        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(AuthClaimTypes.Subject, KnownUser.Id.ToString()),
                new Claim(AuthClaimTypes.AccountType, KnownUser.AccountType.ToString())
            }),
            IssuedAt = issuedAt ?? now,
            NotBefore = issuedAt ?? now,
            Expires = expires ?? now.AddMinutes(5),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        });
    }

    private sealed class SingleUserIdentityService : IIdentityUserService
    {
        private readonly AuthenticatedUser _user;
        private readonly IReadOnlyList<string> _roles;

        public SingleUserIdentityService(AuthenticatedUser user, params string[] roles)
        {
            _user = user;
            _roles = roles;
        }

        public Task<CredentialValidationResult> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            Task.FromResult<CredentialValidationResult>(new CredentialValidationFailed());

        public Task<AuthenticatedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == _user.Id ? _user : null);

        public Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(_roles);
    }
}
