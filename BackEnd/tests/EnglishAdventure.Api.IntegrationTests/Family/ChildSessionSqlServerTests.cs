using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.Authentication;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Api.IntegrationTests.Users;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EnglishAdventure.Api.IntegrationTests.Family;

/// <summary>
/// S2.2 child session (PD-010) through the real API on SQL Server: a Parent switches a device to child mode for one
/// profile; the child session can only learn as that profile, and coming back needs the Parent's password.
/// </summary>
public sealed class ChildSessionSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string LoginRoute = "/api/v1/auth/login";
    private const string RefreshRoute = "/api/v1/auth/refresh";
    private const string MeRoute = "/api/v1/auth/me";
    private const string ChildSessionRoute = "/api/v1/auth/child-session";
    private const string ChildrenRoute = "/api/v1/family/children";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public ChildSessionSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient Client => _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [SqlServerFact]
    public async Task A_child_session_learns_as_one_profile_and_carries_no_parent_rights()
    {
        var (parent, tokens) = await SignInParentAsync();
        var profileId = await CreateProfileAsync(tokens.AccessToken, "Bin", "owl");

        var response = await SendAsync(HttpMethod.Post, ChildSessionRoute, tokens.AccessToken, new { childProfileId = profileId, tokens.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var child = await ReadTokensAsync(response);
        var jwt = new JsonWebToken(child.AccessToken);
        Assert.Equal(parent.Id.ToString(), jwt.Subject);
        Assert.Equal("child", jwt.GetClaim(AuthClaimTypes.Session).Value);
        Assert.Equal(profileId, jwt.GetClaim(AuthClaimTypes.ChildProfileId).Value);
        Assert.False(jwt.TryGetClaim(AuthClaimTypes.Role, out _));

        var me = await ReadAsync(await SendAsync(HttpMethod.Get, MeRoute, child.AccessToken));
        Assert.Equal("child", me.GetProperty("session").GetString());
        Assert.Equal("Bin", me.GetProperty("childProfile").GetProperty("nickname").GetString());
        Assert.Equal("owl", me.GetProperty("childProfile").GetProperty("avatarKey").GetString());
        Assert.Equal(["learning.play"], me.GetProperty("permissions").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(0, me.GetProperty("roles").GetArrayLength());

        // Nothing of the Parent's, the family's or the administration's is reachable.
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, ChildrenRoute, child.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/v1/admin/users", child.AccessToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendAsync(HttpMethod.Post, ChildSessionRoute, child.AccessToken, new { childProfileId = profileId })).StatusCode);
    }

    [SqlServerFact]
    public async Task Starting_child_mode_ends_the_parents_session_on_that_device_only()
    {
        var (parent, device) = await SignInParentAsync();
        var otherDevice = await LoginAsync(parent);
        var profileId = await CreateProfileAsync(device.AccessToken, "Bin", "fox");

        await SendAsync(HttpMethod.Post, ChildSessionRoute, device.AccessToken, new { childProfileId = profileId, device.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(device.RefreshToken!)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(otherDevice.RefreshToken!)).StatusCode);
    }

    [SqlServerFact]
    public async Task A_child_session_stays_a_child_session_when_refreshed_until_the_profile_is_archived()
    {
        var (_, tokens) = await SignInParentAsync();
        var profileId = await CreateProfileAsync(tokens.AccessToken, "Bin", "fox");
        var child = await ReadTokensAsync(await SendAsync(HttpMethod.Post, ChildSessionRoute, tokens.AccessToken, new { childProfileId = profileId }));

        var refreshed = await ReadTokensAsync(await RefreshAsync(child.RefreshToken!));
        var me = await ReadAsync(await SendAsync(HttpMethod.Get, MeRoute, refreshed.AccessToken));
        Assert.Equal("child", me.GetProperty("session").GetString());
        Assert.Equal(profileId, new JsonWebToken(refreshed.AccessToken).GetClaim(AuthClaimTypes.ChildProfileId).Value);

        // The Parent archives the profile (here with the session kept on another device).
        await SendAsync(HttpMethod.Post, $"{ChildrenRoute}/{profileId}/archive", tokens.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refreshed.RefreshToken!)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, MeRoute, refreshed.AccessToken)).StatusCode);
    }

    [SqlServerFact]
    public async Task Only_an_active_profile_of_the_own_family_can_start_child_mode()
    {
        var (_, owner) = await SignInParentAsync();
        var (_, stranger) = await SignInParentAsync();
        var profileId = await CreateProfileAsync(owner.AccessToken, "Bin", "fox");

        var otherFamily = await SendAsync(HttpMethod.Post, ChildSessionRoute, stranger.AccessToken, new { childProfileId = profileId });
        await SendAsync(HttpMethod.Post, $"{ChildrenRoute}/{profileId}/archive", owner.AccessToken);
        var archived = await SendAsync(HttpMethod.Post, ChildSessionRoute, owner.AccessToken, new { childProfileId = profileId });

        Assert.Equal(HttpStatusCode.NotFound, otherFamily.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, archived.StatusCode);
    }

    [SqlServerFact]
    public async Task An_administrator_cannot_start_child_mode()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var admin = await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.SuperAdmin);
        var tokens = await LoginAsync(admin);

        var response = await SendAsync(HttpMethod.Post, ChildSessionRoute, tokens.AccessToken, new { childProfileId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SqlServerFact]
    public async Task The_web_app_switches_its_cookie_to_the_child_session()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var parent = await _fixture.CreateUserAsync(UserType.Parent, roles: SystemRoles.Parent);
        var login = await WebLoginAsync(parent);
        var parentCookie = CookieFrom(login);
        var accessToken = (await ReadAsync(login)).GetProperty("accessToken").GetString()!;
        var profileId = await CreateProfileAsync(accessToken, "Bin", "fox");

        using var request = new HttpRequestMessage(HttpMethod.Post, ChildSessionRoute)
        {
            Content = JsonContent.Create(new { childProfileId = profileId })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add(RefreshTokenCookie.ClientHeaderName, RefreshTokenCookie.WebClientValue);
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.CookieName}={parentCookie}");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await ReadAsync(response)).GetProperty("refreshToken").ValueKind);
        var childCookie = CookieFrom(response);
        Assert.NotEqual(parentCookie, childCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Uri.UnescapeDataString(parentCookie))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(Uri.UnescapeDataString(childCookie))).StatusCode);
    }

    private async Task<(TestUser Parent, TokensBody Tokens)> SignInParentAsync()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var parent = await _fixture.CreateUserAsync(UserType.Parent, roles: SystemRoles.Parent);
        return (parent, await LoginAsync(parent));
    }

    private async Task<TokensBody> LoginAsync(TestUser user)
    {
        var response = await Client.PostAsJsonAsync(LoginRoute, new { user.Email, user.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadTokensAsync(response);
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

    private async Task<string> CreateProfileAsync(string accessToken, string nickname, string avatarKey)
    {
        var response = await SendAsync(HttpMethod.Post, ChildrenRoute, accessToken, new { nickname, avatarKey });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync(response)).GetProperty("id").GetString()!;
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        Client.PostAsJsonAsync(RefreshRoute, new { refreshToken });

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, route)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await Client.SendAsync(request);
    }

    private static string CookieFrom(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
        var setCookie = Assert.Single(values, value => value.StartsWith($"{RefreshTokenCookie.CookieName}=", StringComparison.Ordinal));
        return setCookie[(RefreshTokenCookie.CookieName.Length + 1)..setCookie.IndexOf(';')];
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<TokensBody> ReadTokensAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokensBody>();
        Assert.NotNull(body);
        return body;
    }

    private sealed record TokensBody(string AccessToken, string? RefreshToken);
}
