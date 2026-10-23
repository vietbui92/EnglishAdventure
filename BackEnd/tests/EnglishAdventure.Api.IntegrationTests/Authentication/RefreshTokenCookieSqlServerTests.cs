using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.Authentication;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Api.IntegrationTests.Authentication;

/// <summary>
/// The web client's refresh-token cookie (B-F1): set on login, rotated on refresh, cleared on logout, and read only
/// when the client header is present.
/// </summary>
public sealed class RefreshTokenCookieSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LoginRoute = "/api/v1/auth/login";
    private const string RefreshRoute = "/api/v1/auth/refresh";
    private const string LogoutRoute = "/api/v1/auth/logout";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public RefreshTokenCookieSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // Cookies are handled by hand: the cookie is Secure and the test server runs on http.
    private HttpClient Client => _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = false
    });

    [SqlServerFact]
    public async Task Web_login_sets_an_httponly_cookie_and_leaves_the_token_out_of_the_body()
    {
        var user = await _fixture.CreateUserAsync(UserType.Parent);

        var response = await WebLoginAsync(user);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var setCookie = GetRefreshSetCookie(response);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"path={RefreshTokenCookie.CookiePath}", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal(JsonValueKind.Null, body.GetProperty("refreshToken").ValueKind);
    }

    [SqlServerFact]
    public async Task Web_refresh_reads_the_cookie_and_rotates_it()
    {
        var user = await _fixture.CreateUserAsync(UserType.Parent);
        var loginCookie = ReadCookieValue(await WebLoginAsync(user));

        var response = await Client.SendAsync(WebRequest(RefreshRoute, loginCookie));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotatedCookie = ReadCookieValue(response);
        Assert.NotEqual(loginCookie, rotatedCookie);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("refreshToken").ValueKind);

        var replay = await Client.SendAsync(WebRequest(RefreshRoute, loginCookie));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [SqlServerFact]
    public async Task Web_refresh_without_a_cookie_is_unauthorized()
    {
        var response = await Client.SendAsync(WebRequest(RefreshRoute, cookie: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SqlServerFact]
    public async Task Cookie_is_ignored_without_the_client_header()
    {
        var user = await _fixture.CreateUserAsync(UserType.Parent);
        var cookie = ReadCookieValue(await WebLoginAsync(user));

        using var request = new HttpRequestMessage(HttpMethod.Post, RefreshRoute);
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.CookieName}={cookie}");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var token = Assert.Single(await LoadTokensAsync(user.Id));
        Assert.Null(token.RevokedAtUtc);
    }

    [SqlServerFact]
    public async Task Web_logout_revokes_the_session_and_deletes_the_cookie()
    {
        var user = await _fixture.CreateUserAsync(UserType.Parent);
        var cookie = ReadCookieValue(await WebLoginAsync(user));

        var response = await Client.SendAsync(WebRequest(LogoutRoute, cookie));
        var withoutCookie = await Client.SendAsync(WebRequest(LogoutRoute, cookie: null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", GetRefreshSetCookie(response), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NoContent, withoutCookie.StatusCode);
        var token = Assert.Single(await LoadTokensAsync(user.Id));
        Assert.Equal(RefreshTokenRevocationReason.Revoked, token.RevocationReason);
    }

    private async Task<HttpResponseMessage> WebLoginAsync(TestUser user)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, LoginRoute)
        {
            Content = JsonContent.Create(new { user.Email, user.Password })
        };
        request.Headers.Add(RefreshTokenCookie.ClientHeaderName, RefreshTokenCookie.WebClientValue);
        return await Client.SendAsync(request);
    }

    private static HttpRequestMessage WebRequest(string route, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add(RefreshTokenCookie.ClientHeaderName, RefreshTokenCookie.WebClientValue);
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshTokenCookie.CookieName}={cookie}");
        }

        return request;
    }

    private static string GetRefreshSetCookie(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
        return Assert.Single(values, value => value.StartsWith($"{RefreshTokenCookie.CookieName}=", StringComparison.Ordinal));
    }

    private static string ReadCookieValue(HttpResponseMessage response)
    {
        var setCookie = GetRefreshSetCookie(response);
        var value = setCookie[(RefreshTokenCookie.CookieName.Length + 1)..setCookie.IndexOf(';')];
        Assert.False(string.IsNullOrEmpty(value));
        return value;
    }

    private Task<List<RefreshToken>> LoadTokensAsync(Guid userId) =>
        _fixture.WithDbContextAsync(db => db.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == userId)
            .ToListAsync());
}
