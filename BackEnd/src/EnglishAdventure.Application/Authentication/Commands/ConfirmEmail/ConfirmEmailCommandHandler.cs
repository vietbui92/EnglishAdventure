using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Users;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IEmailConfirmationTokenService _confirmationTokens;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ConfirmEmailCommandHandler(
        IEmailConfirmationTokenService confirmationTokens,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _confirmationTokens = confirmationTokens;
        _users = users;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = await _confirmationTokens.ConsumeAsync(request.Token, cancellationToken);
        var user = userId is null ? null : await _users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            // Unknown and expired links get the same answer.
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Token), "The confirmation link is invalid or has expired.")]);
        }

        user.ConfirmEmail(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
