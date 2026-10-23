namespace EnglishAdventure.Infrastructure.Identity;

/// <summary>
/// Server-side state of one refresh token. Only the SHA-256 hash of the raw token is stored.
/// All tokens produced by rotating the same login share a <see cref="FamilyId"/> (one family = one session).
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid FamilyId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public RefreshTokenRevocationReason? RevocationReason { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
