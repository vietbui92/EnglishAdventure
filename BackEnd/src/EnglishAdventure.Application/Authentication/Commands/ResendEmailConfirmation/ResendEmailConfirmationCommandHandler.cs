using EnglishAdventure.Application.Common.Email;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Users;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.ResendEmailConfirmation;

/// <summary>
/// Sends a new confirmation link to an unconfirmed account. Does the same visible nothing for any other email, so the
/// endpoint cannot be used to find out who is registered.
/// </summary>
public sealed class ResendEmailConfirmationCommandHandler : IRequestHandler<ResendEmailConfirmationCommand>
{
    private readonly IUserRepository _users;
    private readonly IEmailConfirmationTokenService _confirmationTokens;
    private readonly IEmailSender _emailSender;
    private readonly IAppLinks _links;
    private readonly IUnitOfWork _unitOfWork;

    public ResendEmailConfirmationCommandHandler(
        IUserRepository users,
        IEmailConfirmationTokenService confirmationTokens,
        IEmailSender emailSender,
        IAppLinks links,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _confirmationTokens = confirmationTokens;
        _emailSender = emailSender;
        _links = links;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.IsEmailConfirmed || !user.IsActive)
        {
            return;
        }

        var token = await _confirmationTokens.IssueAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _emailSender.SendAsync(
            AuthEmails.ConfirmEmail(
                user.Email,
                request.Language ?? AuthEmails.DefaultLanguage,
                _links.ConfirmEmail(token),
                EmailConfirmation.LinkLifetime),
            cancellationToken);
    }
}
