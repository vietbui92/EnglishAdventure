using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Common.Email;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Users;
using EnglishAdventure.Domain.Entities.Identity;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.RegisterParent;

/// <summary>
/// Public Parent registration (PD-009). Every outcome looks the same to the caller, so the endpoint never reveals
/// whether an email is registered: a new email gets a confirmation link, an unconfirmed one a fresh link, and a
/// confirmed one a note that the account already exists.
/// </summary>
public sealed class RegisterParentCommandHandler : IRequestHandler<RegisterParentCommand>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailConfirmationTokenService _confirmationTokens;
    private readonly IEmailSender _emailSender;
    private readonly IAppLinks _links;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RegisterParentCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IEmailConfirmationTokenService confirmationTokens,
        IEmailSender emailSender,
        IAppLinks links,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _confirmationTokens = confirmationTokens;
        _emailSender = emailSender;
        _links = links;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(RegisterParentCommand request, CancellationToken cancellationToken)
    {
        var language = request.Language ?? AuthEmails.DefaultLanguage;

        // Hashed on every path, so the response time does not reveal whether the email exists.
        var passwordHash = _passwordHasher.Hash(request.Password);

        var existing = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            await NotifyExistingAsync(existing, language, cancellationToken);
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var user = User.RegisterParent(request.Email, passwordHash, Consent.CurrentVersion, now);
        var parentRole = await _roles.GetByNameAsync(SystemRoles.Parent, cancellationToken)
            ?? throw new InvalidOperationException("The Parent role does not exist. Run the sync-roles command.");
        user.AssignRole(parentRole.Id, now);
        _users.Add(user);

        var token = await _confirmationTokens.IssueAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SendConfirmationAsync(user, token, language, cancellationToken);
    }

    private async Task NotifyExistingAsync(User existing, string language, CancellationToken cancellationToken)
    {
        // The first registration stays; registering again only sends a new link. The password is not changed,
        // because whoever registers again has not proven they own the email.
        if (!existing.IsEmailConfirmed && existing.IsActive)
        {
            var token = await _confirmationTokens.IssueAsync(existing.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await SendConfirmationAsync(existing, token, language, cancellationToken);
            return;
        }

        await _emailSender.SendAsync(
            AuthEmails.AlreadyRegistered(existing.Email, language, _links.SignIn()),
            cancellationToken);
    }

    private Task SendConfirmationAsync(User user, string token, string language, CancellationToken cancellationToken) =>
        _emailSender.SendAsync(
            AuthEmails.ConfirmEmail(user.Email, language, _links.ConfirmEmail(token), EmailConfirmation.LinkLifetime),
            cancellationToken);
}
