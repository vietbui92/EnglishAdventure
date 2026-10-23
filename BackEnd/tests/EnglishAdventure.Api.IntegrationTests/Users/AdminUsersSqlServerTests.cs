using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Users;

/// <summary>
/// S1.3 user management through the real API on SQL Server, signed in as a SuperAdmin.
/// </summary>
public sealed class AdminUsersSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string UsersRoute = "/api/v1/admin/users";
    private const string StrongPassword = "Adventure#2026";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public AdminUsersSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task User_management_requires_the_matching_permissions()
    {
        var contentAdmin = await SignInWithRoleAsync(SystemRoles.ContentAdmin);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _fixture.Factory.CreateClient().GetAsync(UsersRoute)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await contentAdmin.GetAsync(UsersRoute)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await contentAdmin.PostAsync(UsersRoute, NewUser("Parent"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await contentAdmin.PutAsync($"{UsersRoute}/{Guid.NewGuid()}/roles", new { roles = Array.Empty<string>() })).StatusCode);
    }

    [SqlServerFact]
    public async Task A_Parent_user_is_created_with_the_Parent_role_and_can_sign_in()
    {
        var superAdmin = await SignInWithRoleAsync(SystemRoles.SuperAdmin);
        var request = NewUser("Parent");

        var response = await superAdmin.PostAsync(UsersRoute, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var user = await ReadAsync(response);
        Assert.Equal("Parent", user.GetProperty("userType").GetString());
        Assert.Equal([SystemRoles.Parent], Roles(user));

        var login = await AdminApiClient.LoginAsync(_fixture, request.email, request.password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // The same email in another case is a duplicate.
        var duplicate = await superAdmin.PostAsync(UsersRoute, request with { email = request.email.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [SqlServerFact]
    public async Task Invalid_new_users_are_rejected_with_400()
    {
        var superAdmin = await SignInWithRoleAsync(SystemRoles.SuperAdmin);

        Assert.Equal(HttpStatusCode.BadRequest, (await superAdmin.PostAsync(UsersRoute, NewUser("Child"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await superAdmin.PostAsync(UsersRoute, NewUser("Parent") with { password = "weak" })).StatusCode);
    }

    [SqlServerFact]
    public async Task An_Admin_user_starts_without_roles_and_gets_permissions_from_assigned_roles()
    {
        var superAdmin = await SignInWithRoleAsync(SystemRoles.SuperAdmin);
        var request = NewUser("Admin");
        var created = await ReadAsync(await superAdmin.PostAsync(UsersRoute, request));
        Assert.Empty(Roles(created));
        var userId = created.GetProperty("id").GetGuid();

        var updated = await superAdmin.PutAsync($"{UsersRoute}/{userId}/roles", new { roles = new[] { SystemRoles.ContentAdmin } });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal([SystemRoles.ContentAdmin], Roles(await ReadAsync(updated)));

        var newAdmin = await AdminApiClient.SignInAsync(_fixture, new TestUser(userId, request.email, request.password));
        var me = await ReadAsync(await newAdmin.GetAsync("/api/v1/auth/me"));
        Assert.Contains(Permissions.Content.Manage, me.GetProperty("permissions").EnumerateArray().Select(code => code.GetString()));
        Assert.Equal(HttpStatusCode.Forbidden, (await newAdmin.GetAsync(UsersRoute)).StatusCode);
    }

    [SqlServerFact]
    public async Task Safety_rules_reject_self_changes_wrong_roles_and_unknown_targets()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var self = await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.SuperAdmin);
        var superAdmin = await AdminApiClient.SignInAsync(_fixture, self);
        var parent = await ReadAsync(await superAdmin.PostAsync(UsersRoute, NewUser("Parent")));
        var parentId = parent.GetProperty("id").GetGuid();

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await superAdmin.PutAsync($"{UsersRoute}/{self.Id}/roles", new { roles = Array.Empty<string>() })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await superAdmin.PostAsync($"{UsersRoute}/{self.Id}/deactivate")).StatusCode);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await superAdmin.PutAsync($"{UsersRoute}/{parentId}/roles", new { roles = new[] { SystemRoles.SuperAdmin } })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await superAdmin.PutAsync($"{UsersRoute}/{parentId}/roles", new { roles = new[] { "NoSuchRole" } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await superAdmin.GetAsync($"{UsersRoute}/{Guid.NewGuid()}")).StatusCode);
    }

    [SqlServerFact]
    public async Task Deactivation_ends_sessions_and_blocks_sign_in_until_activated_again()
    {
        var superAdmin = await SignInWithRoleAsync(SystemRoles.SuperAdmin);
        var request = NewUser("Parent");
        var userId = (await ReadAsync(await superAdmin.PostAsync(UsersRoute, request))).GetProperty("id").GetGuid();
        var tokens = await ReadAsync(await AdminApiClient.LoginAsync(_fixture, request.email, request.password));

        Assert.Equal(HttpStatusCode.NoContent, (await superAdmin.PostAsync($"{UsersRoute}/{userId}/deactivate")).StatusCode);

        var refresh = await _fixture.Factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = tokens.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AdminApiClient.LoginAsync(_fixture, request.email, request.password)).StatusCode);
        Assert.False((await ReadAsync(await superAdmin.GetAsync($"{UsersRoute}/{userId}"))).GetProperty("isActive").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await superAdmin.PostAsync($"{UsersRoute}/{userId}/activate")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AdminApiClient.LoginAsync(_fixture, request.email, request.password)).StatusCode);
    }

    [SqlServerFact]
    public async Task Users_can_be_searched_and_paged()
    {
        var superAdmin = await SignInWithRoleAsync(SystemRoles.SuperAdmin);
        var prefix = $"page{Guid.NewGuid():N}"[..16];
        foreach (var name in new[] { "c", "a", "b" })
        {
            var created = await superAdmin.PostAsync(UsersRoute, NewUser("Parent") with { email = $"{prefix}.{name}@example.test" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        var page = await ReadAsync(await superAdmin.GetAsync($"{UsersRoute}?search={prefix}&pageSize=2"));

        Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(
            [$"{prefix}.a@example.test", $"{prefix}.b@example.test"],
            page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("email").GetString()));
    }

    private async Task<AdminApiClient> SignInWithRoleAsync(string roleName)
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        var user = await _fixture.CreateUserAsync(UserType.Admin, roles: roleName);
        return await AdminApiClient.SignInAsync(_fixture, user);
    }

    private static NewUserBody NewUser(string userType) =>
        new($"{Guid.NewGuid():N}@example.test", StrongPassword, userType);

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static IEnumerable<string?> Roles(JsonElement user) =>
        user.GetProperty("roles").EnumerateArray().Select(role => role.GetString());

#pragma warning disable IDE1006 // JSON property names
    private sealed record NewUserBody(string email, string password, string userType);
#pragma warning restore IDE1006
}
