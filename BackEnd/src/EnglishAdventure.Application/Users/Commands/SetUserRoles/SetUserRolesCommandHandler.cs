using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Exceptions;
using MediatR;

namespace EnglishAdventure.Application.Users.Commands.SetUserRoles;

public sealed class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetUserRolesCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IUserContext userContext,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _roles = roles;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.Id == _userContext.UserId)
        {
            throw new DomainException("You cannot change your own roles.");
        }

        var requestedRoles = await _roles.ListByNamesAsync(request.RoleNames, cancellationToken);
        var unknown = request.RoleNames
            .Except(requestedRoles.Select(role => role.Name), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknown.Count > 0)
        {
            throw new NotFoundException($"Unknown role(s): {string.Join(", ", unknown)}.");
        }

        UserRoleRules.EnsureAllowed(user.UserType, requestedRoles.Select(role => role.Name));

        var requestedRoleIds = requestedRoles.Select(role => role.Id).ToHashSet();
        var superAdminRole = await _roles.GetByNameAsync(SystemRoles.SuperAdmin, cancellationToken);
        if (superAdminRole is not null
            && user.IsActive
            && user.HasRole(superAdminRole.Id)
            && !requestedRoleIds.Contains(superAdminRole.Id)
            && !await _users.AnyOtherActiveUserWithRoleAsync(superAdminRole.Id, user.Id, cancellationToken))
        {
            throw new DomainException("The SuperAdmin role cannot be removed from the last active SuperAdmin.");
        }

        foreach (var removedRoleId in user.Roles.Select(role => role.RoleId).Except(requestedRoleIds).ToList())
        {
            user.RemoveRole(removedRoleId);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var roleId in requestedRoleIds)
        {
            user.AssignRole(roleId, now, _userContext.UserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
