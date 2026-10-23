using System.Net;
using EnglishAdventure.Api.IntegrationTests.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Api.IntegrationTests.Users;

/// <summary>
/// The last active SuperAdmin cannot be deactivated or lose the SuperAdmin role. Runs in its own throwaway database so
/// that exactly one SuperAdmin exists; the caller manages users through a custom role.
/// </summary>
public sealed class LastSuperAdminSqlServerTests : IClassFixture<SqlServerAuthDatabaseFixture>
{
    private const string UsersRoute = "/api/v1/admin/users";

    private readonly SqlServerAuthDatabaseFixture _fixture;

    public LastSuperAdminSqlServerTests(SqlServerAuthDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task The_last_active_SuperAdmin_is_protected_until_another_one_exists()
    {
        await AdminApiClient.SyncSystemRolesAsync(_fixture);
        await _fixture.GrantPermissionsAsync("UsersOfficer", Permissions.Users.Manage, Permissions.Roles.Manage);
        var onlySuperAdmin = await _fixture.CreateUserAsync(UserType.Admin, roles: SystemRoles.SuperAdmin);
        var otherAdmin = await _fixture.CreateUserAsync(UserType.Admin);
        var officer = await AdminApiClient.SignInAsync(
            _fixture,
            await _fixture.CreateUserAsync(UserType.Admin, roles: "UsersOfficer"));

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await officer.PostAsync($"{UsersRoute}/{onlySuperAdmin.Id}/deactivate")).StatusCode);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await officer.PutAsync($"{UsersRoute}/{onlySuperAdmin.Id}/roles", new { roles = Array.Empty<string>() })).StatusCode);

        // Once a second SuperAdmin exists, the first one may be deactivated.
        Assert.Equal(
            HttpStatusCode.OK,
            (await officer.PutAsync($"{UsersRoute}/{otherAdmin.Id}/roles", new { roles = new[] { SystemRoles.SuperAdmin } })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await officer.PostAsync($"{UsersRoute}/{onlySuperAdmin.Id}/deactivate")).StatusCode);
    }
}
