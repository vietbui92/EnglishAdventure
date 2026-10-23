using System.Collections.Concurrent;
using EnglishAdventure.Application.Common.Email;

namespace EnglishAdventure.Api.IntegrationTests;

/// <summary>Keeps sent emails in memory instead of writing files, so tests can read the links.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> Sent => _sent.ToArray();

    public IReadOnlyList<EmailMessage> SentTo(string address) =>
        _sent.Where(message => string.Equals(message.To, address, StringComparison.OrdinalIgnoreCase)).ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
