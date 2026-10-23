using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using MediatR;

namespace EnglishAdventure.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateUserCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IUserContext userContext,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userType = Enum.Parse<UserType>(request.UserType, ignoreCase: true);

        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var user = User.Create(request.Email, _passwordHasher.Hash(request.Password), userType, now);

        if (userType == UserType.Parent)
        {
            var parentRole = await _roles.GetByNameAsync(SystemRoles.Parent, cancellationToken)
                ?? throw new InvalidOperationException("The Parent role does not exist. Run the sync-roles command.");

            user.AssignRole(parentRole.Id, now, _userContext.UserId);
        }

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
