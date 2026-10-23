namespace EnglishAdventure.Infrastructure.Identity;

/// <summary>
/// One link sent to confirm a registered Parent's email. Only the SHA-256 hash of the raw token is stored, and a token
/// works once, until <see cref="ExpiresAtUtc"/>.
/// </summary>
public sealed class EmailConfirmationToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }
}
