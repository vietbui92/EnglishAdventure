using System.Text;
using EnglishAdventure.Application.Common.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishAdventure.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Folder that receives each email as a text file, relative to the working directory or absolute. The production
    /// email provider is a later decision (plan step S1.4); until then this is the only sender.
    /// </summary>
    public string PickupDirectory { get; set; } = "App_Data/emails";
}

/// <summary>
/// Development email sender: writes every email to a file in <see cref="EmailOptions.PickupDirectory"/> instead of
/// sending it. Open the newest file to follow a confirmation link. The log names the file, never the link or the
/// recipient.
/// </summary>
internal sealed class PickupDirectoryEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PickupDirectoryEmailSender> _logger;

    public PickupDirectoryEmailSender(
        IOptions<EmailOptions> options,
        TimeProvider timeProvider,
        ILogger<PickupDirectoryEmailSender> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var directory = Path.GetFullPath(_options.PickupDirectory);
        Directory.CreateDirectory(directory);

        var fileName = $"{_timeProvider.GetUtcNow():yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.txt";
        var path = Path.Combine(directory, fileName);
        var content = $"To: {message.To}\nSubject: {message.Subject}\n\n{message.TextBody}\n";

        // UTF-8 without a byte order mark, so the first line reads "To:" in every editor.
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
        _logger.LogInformation("Email \"{Subject}\" written to {Path}", message.Subject, path);
    }
}
