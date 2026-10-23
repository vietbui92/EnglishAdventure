using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Exceptions;
using MediatR;

namespace EnglishAdventure.Application.Users.Commands.SetUserActive;

public sealed class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserActiveCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenService refreshTokens,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (request.IsActive)
        {
            user.Activate();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (user.Id == _userContext.UserId)
        {
            throw new DomainException("You cannot deactivate your own user.");
        }

        var superAdminRole = await _roles.GetByNameAsync(SystemRoles.SuperAdmin, cancellationToken);
        if (superAdminRole is not null
            && user.IsActive
            && user.HasRole(superAdminRole.Id)
            && !await _users.AnyOtherActiveUserWithRoleAsync(superAdminRole.Id, user.Id, cancellationToken))
        {
            throw new DomainException("The last active SuperAdmin cannot be deactivated.");
        }

        user.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
    }
}
