using EnglishAdventure.Application.Users;
using EnglishAdventure.Application.Users.Commands.CreateUser;
using EnglishAdventure.Application.Users.Commands.SetUserRoles;
using EnglishAdventure.Application.Users.Queries.ListUsers;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Application.UnitTests.Users;

public sealed class UserValidationTests
{
    private const string StrongPassword = "Adventure#2026";

    [Theory]
    [InlineData("Admin")]
    [InlineData("parent")]
    public void CreateUser_accepts_Admin_and_Parent(string userType)
    {
        var result = new CreateUserCommandValidator()
            .Validate(new CreateUserCommand("someone@example.test", StrongPassword, userType));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Child")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    public void CreateUser_rejects_other_user_types(string userType)
    {
        var result = new CreateUserCommandValidator()
            .Validate(new CreateUserCommand("someone@example.test", StrongPassword, userType));

        var error = Assert.Single(result.Errors);
        Assert.Contains("Child users are not available yet", error.ErrorMessage);
    }

    [Fact]
    public void CreateUser_applies_the_password_rule_and_hides_the_password_in_ToString()
    {
        var command = new CreateUserCommand("someone@example.test", "short", "Parent");

        Assert.False(new CreateUserCommandValidator().Validate(command).IsValid);
        Assert.DoesNotContain("short", command.ToString());
    }

    [Fact]
    public void SetUserRoles_rejects_duplicates_but_accepts_an_empty_list()
    {
        var validator = new SetUserRolesCommandValidator();

        Assert.True(validator.Validate(new SetUserRolesCommand(Guid.NewGuid(), [])).IsValid);
        Assert.False(validator.Validate(new SetUserRolesCommand(Guid.NewGuid(), ["ContentAdmin", "contentadmin"])).IsValid);
        Assert.False(validator.Validate(new SetUserRolesCommand(Guid.NewGuid(), [" "])).IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void ListUsers_rejects_invalid_paging(int page, int pageSize)
    {
        Assert.False(new ListUsersQueryValidator().Validate(new ListUsersQuery(page, pageSize)).IsValid);
    }

    [Fact]
    public void A_Parent_user_can_only_hold_the_Parent_role_and_an_Admin_user_never_holds_it()
    {
        UserRoleRules.EnsureAllowed(UserType.Parent, ["Parent"]);
        UserRoleRules.EnsureAllowed(UserType.Admin, ["SuperAdmin", "ContentAdmin", "CustomRole"]);

        Assert.Throws<DomainException>(() => UserRoleRules.EnsureAllowed(UserType.Parent, ["SuperAdmin"]));
        Assert.Throws<DomainException>(() => UserRoleRules.EnsureAllowed(UserType.Admin, ["Parent"]));
    }
}
