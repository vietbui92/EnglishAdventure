using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Domain.Enums;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserProfile>
{
    private readonly IUserContext _userContext;
    private readonly IIdentityUserService _identityUserService;
    private readonly IUserPermissionReader _permissionReader;
    private readonly IChildProfileQueries _childProfiles;

    public GetCurrentUserQueryHandler(
        IUserContext userContext,
        IIdentityUserService identityUserService,
        IUserPermissionReader permissionReader,
        IChildProfileQueries childProfiles)
    {
        _userContext = userContext;
        _identityUserService = identityUserService;
        _permissionReader = permissionReader;
        _childProfiles = childProfiles;
    }

    public async Task<CurrentUserProfile> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId is not { } userId)
        {
            throw new UnauthorizedException();
        }

        var user = await _identityUserService.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException();
        }

        if (_userContext.ChildProfileId is { } childProfileId)
        {
            // A child session: the child's profile and the child-session permissions, never the Parent's roles.
            var child = await _childProfiles.GetAsync(childProfileId, userId, cancellationToken);
            if (child is null || child.Status != nameof(ChildProfileStatus.Active))
            {
                throw new UnauthorizedException();
            }

            var childPermissions = await _permissionReader.GetChildSessionPermissionsAsync(userId, cancellationToken);

            return new CurrentUserProfile(
                user.Id,
                user.Email,
                user.UserType,
                [],
                childPermissions.Order(StringComparer.Ordinal).ToArray(),
                CurrentUserProfile.ChildSession,
                child);
        }

        var roles = await _identityUserService.GetRolesAsync(userId, cancellationToken);
        var permissions = await _permissionReader.GetPermissionsAsync(userId, cancellationToken);

        return new CurrentUserProfile(
            user.Id,
            user.Email,
            user.UserType,
            roles,
            permissions.Order(StringComparer.Ordinal).ToArray(),
            CurrentUserProfile.UserSession,
            ChildProfile: null);
    }
}
