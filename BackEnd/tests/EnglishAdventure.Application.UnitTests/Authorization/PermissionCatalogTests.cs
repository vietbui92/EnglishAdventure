using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Authorization.Queries.GetPermissionCatalog;
using EnglishAdventure.Domain.Entities.Identity;

namespace EnglishAdventure.Application.UnitTests.Authorization;

public sealed class PermissionCatalogTests
{
    private static IEnumerable<string> Codes => Permissions.All.Select(permission => permission.Code);

    [Fact]
    public void Permission_codes_are_unique_valid_and_described()
    {
        Assert.Equal(Codes.Count(), Codes.Distinct().Count());

        foreach (var permission in Permissions.All)
        {
            Assert.Equal(permission.Code, Permission.Create(permission.Code, permission.Description).Code);
            Assert.False(string.IsNullOrWhiteSpace(permission.Description));
        }
    }

    [Fact]
    public void IsDefined_recognizes_only_catalog_codes()
    {
        Assert.True(Permissions.IsDefined(Permissions.Content.Manage));
        Assert.False(Permissions.IsDefined("content.delete-everything"));
        Assert.False(Permissions.IsDefined("Content.Manage"));
    }

    [Fact]
    public void System_roles_are_the_three_PD_008_roles_and_grant_only_catalog_permissions()
    {
        Assert.Equal(
            [SystemRoles.SuperAdmin, SystemRoles.ContentAdmin, SystemRoles.Parent],
            SystemRoles.All.Select(role => role.Name));

        Assert.All(SystemRoles.All, role =>
        {
            Assert.NotEmpty(role.Permissions);
            Assert.All(role.Permissions, code => Assert.True(Permissions.IsDefined(code), code));
        });
    }

    [Fact]
    public void SuperAdmin_holds_every_permission_except_family_data()
    {
        var superAdmin = Role(SystemRoles.SuperAdmin);

        Assert.Equal(
            Codes.Where(code => !code.StartsWith("family.", StringComparison.Ordinal)).Order(),
            superAdmin.Order());
    }

    [Fact]
    public void Learning_as_a_child_is_a_session_permission_that_no_role_can_hold()
    {
        Assert.True(Permissions.IsDefined(Permissions.Learning.Play));
        Assert.Contains(Permissions.Learning.Play, Permissions.SessionOnly);
        Assert.DoesNotContain(Permissions.Learning.Play, Codes);
        Assert.All(SystemRoles.All, role => Assert.DoesNotContain(Permissions.Learning.Play, role.Permissions));
    }

    [Fact]
    public void ContentAdmin_and_Parent_follow_least_privilege()
    {
        Assert.Equal(
            [Permissions.Content.ReadDrafts, Permissions.Content.Manage, Permissions.Content.Publish],
            Role(SystemRoles.ContentAdmin));
        Assert.Equal(
            [Permissions.Family.ManageChildren, Permissions.Family.ReadProgress],
            Role(SystemRoles.Parent));
    }

    [Fact]
    public async Task GetPermissionCatalog_returns_the_whole_catalog()
    {
        var catalog = await new GetPermissionCatalogQueryHandler()
            .Handle(new GetPermissionCatalogQuery(), CancellationToken.None);

        Assert.Equal(Permissions.All, catalog);
    }

    private static IReadOnlyList<string> Role(string name) =>
        SystemRoles.All.Single(role => role.Name == name).Permissions;
}
