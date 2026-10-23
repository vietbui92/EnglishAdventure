using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Api.IntegrationTests.Authorization;

/// <summary>
/// Permission checks end to end on SQL Server: user → roles → role permissions, read on each request.
/// </summary>
public sealed class PermissionAuthorizationSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string PermissionCatalogRoute = "/api/v1/admin/permissions";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public PermissionAuthorizationSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task An_user_whose_role_grants_the_permission_is_allowed_and_sees_it_in_me()
    {
        var roleName = UniqueRoleName();
        await _fixture.GrantPermissionsAsync(roleName, Permissions.Roles.Manage, Permissions.Users.Read);
        var user = await _fixture.CreateUserAsync(UserType.Admin, roles: roleName);
        var accessToken = await LoginAsync(user);

        var catalog = await SendAsync(PermissionCatalogRoute, accessToken);
        var me = await SendAsync("/api/v1/auth/me", accessToken);

        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        var body = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            [Permissions.Roles.Manage, Permissions.Users.Read],
            body.GetProperty("permissions").EnumerateArray().Select(permission => permission.GetString()));
    }

    [SqlServerFact]
    public async Task An_user_whose_roles_lack_the_permission_gets_403()
    {
        var roleName = UniqueRoleName();
        await _fixture.GrantPermissionsAsync(roleName, Permissions.Content.Manage);
        var user = await _fixture.CreateUserAsync(UserType.Admin, roles: roleName);

        var response = await SendAsync(PermissionCatalogRoute, await LoginAsync(user));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SqlServerFact]
    public async Task A_deactivated_user_loses_its_permissions_while_its_token_is_still_valid()
    {
        var roleName = UniqueRoleName();
        await _fixture.GrantPermissionsAsync(roleName, Permissions.Roles.Manage);
        var user = await _fixture.CreateUserAsync(UserType.Admin, roles: roleName);
        var accessToken = await LoginAsync(user);

        await _fixture.WithDbContextAsync(db => db.Users
            .Where(candidate => candidate.Id == user.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.IsActive, false)));

        var response = await SendAsync(PermissionCatalogRoute, accessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> LoginAsync(TestUser user)
    {
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/login", new { user.Email, user.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> SendAsync(string route, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _fixture.Factory.CreateClient().SendAsync(request);
    }

    private static string UniqueRoleName() => $"Role-{Guid.NewGuid():N}"[..30];
}
