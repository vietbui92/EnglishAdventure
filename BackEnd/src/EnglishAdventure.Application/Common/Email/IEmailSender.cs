namespace EnglishAdventure.Application.Common.Email;

/// <summary>
/// Sends one email. The development implementation writes it to a local folder; the production provider is a later
/// decision (plan step S1.4).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailMessage(string To, string Subject, string TextBody);
