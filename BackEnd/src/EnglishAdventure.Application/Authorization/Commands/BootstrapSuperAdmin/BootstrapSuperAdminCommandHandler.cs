using EnglishAdventure.Application.Users;
using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization.Commands.SyncSystemRoles;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using MediatR;

namespace EnglishAdventure.Application.Authorization.Commands.BootstrapSuperAdmin;

public sealed class BootstrapSuperAdminCommandHandler
    : IRequestHandler<BootstrapSuperAdminCommand, BootstrapSuperAdminResult>
{
    private readonly ISender _sender;
    private readonly IRoleRepository _roles;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public BootstrapSuperAdminCommandHandler(
        ISender sender,
        IRoleRepository roles,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sender = sender;
        _roles = roles;
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<BootstrapSuperAdminResult> Handle(
        BootstrapSuperAdminCommand request,
        CancellationToken cancellationToken)
    {
        var sync = await _sender.Send(new SyncSystemRolesCommand(), cancellationToken);

        var superAdminRole = await _roles.GetByNameAsync(SystemRoles.SuperAdmin, cancellationToken)
            ?? throw new InvalidOperationException("The SuperAdmin role is missing after syncing the system roles.");

        if (await _users.AnyActiveUserWithRoleAsync(superAdminRole.Id, cancellationToken))
        {
            throw new ConflictException(
                "A SuperAdmin already exists. Further SuperAdmins are created by an existing SuperAdmin.");
        }

        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var user = User.Create(request.Email, _passwordHasher.Hash(request.Password), UserType.Admin, now);
        user.AssignRole(superAdminRole.Id, now);

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BootstrapSuperAdminResult(user.Id, sync);
    }
}
