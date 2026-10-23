using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Authentication.Queries.GetCurrentUser;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.UnitTests.Authentication;

public sealed class GetCurrentUserQueryHandlerTests
{
    private readonly FakeUserContext _userContext = new();
    private readonly FakeIdentityUserService _identityUserService = new();
    private readonly FakeUserPermissionReader _permissionReader = new();

    private GetCurrentUserQueryHandler CreateHandler() => new(_userContext, _identityUserService, _permissionReader);

    [Fact]
    public async Task Handle_throws_unauthorized_when_there_is_no_authenticated_user()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(new GetCurrentUserQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_unauthorized_when_the_user_is_inactive()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId = userId;
        _identityUserService.User = new AuthenticatedUser(userId, "a@example.test", UserType.Admin, false, false);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => CreateHandler().Handle(new GetCurrentUserQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_returns_profile_with_roles_and_sorted_permissions_for_the_authenticated_user()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId = userId;
        _identityUserService.User = new AuthenticatedUser(userId, "a@example.test", UserType.Admin, true, false);
        _identityUserService.Roles = new[] { "Admin" };
        _permissionReader.Permissions.UnionWith(["roles.manage", "users.read"]);

        var profile = await CreateHandler().Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.Equal(userId, profile.UserId);
        Assert.Equal("a@example.test", profile.Email);
        Assert.Equal(UserType.Admin, profile.UserType);
        Assert.Equal(new[] { "Admin" }, profile.Roles);
        Assert.Equal(new[] { "roles.manage", "users.read" }, profile.Permissions);
    }
}
