using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.UnitTests.Entities.Identity;

public sealed class RoleAndPermissionTests
{
    [Fact]
    public void Role_requires_a_name_within_the_length_limit()
    {
        Assert.Equal("ContentAdmin", Role.Create("  ContentAdmin ").Name);
        Assert.Throws<DomainException>(() => Role.Create(" "));
        Assert.Throws<DomainException>(() => Role.Create(new string('x', Role.NameMaxLength + 1)));
    }

    [Fact]
    public void Granting_the_same_permission_twice_keeps_one_grant_and_it_can_be_revoked()
    {
        var role = Role.Create("ContentAdmin");
        var permissionId = Guid.NewGuid();

        role.GrantPermission(permissionId);
        role.GrantPermission(permissionId);

        var grant = Assert.Single(role.Permissions);
        Assert.Equal(role.Id, grant.RoleId);
        Assert.Equal(permissionId, grant.PermissionId);

        role.RevokePermission(permissionId);

        Assert.Empty(role.Permissions);
    }

    [Theory]
    [InlineData("content.manage")]
    [InlineData("content.read-drafts")]
    [InlineData("family.children.manage")]
    public void Permission_accepts_area_dot_action_codes(string code)
    {
        Assert.Equal(code, Permission.Create(code).Code);
    }

    [Theory]
    [InlineData("content")]
    [InlineData("Content.Manage")]
    [InlineData("content..manage")]
    [InlineData("content.manage.")]
    [InlineData("content manage")]
    public void Permission_rejects_malformed_codes(string code)
    {
        Assert.Throws<DomainException>(() => Permission.Create(code));
    }
}
