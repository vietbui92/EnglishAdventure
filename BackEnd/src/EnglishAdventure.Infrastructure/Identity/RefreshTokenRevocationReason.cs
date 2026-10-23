namespace EnglishAdventure.Infrastructure.Identity;

public enum RefreshTokenRevocationReason
{
    /// <summary>Consumed by a successful refresh and replaced by a new token.</summary>
    Rotated = 1,

    /// <summary>The session was ended by logout or because the account became ineligible.</summary>
    Revoked = 2,

    /// <summary>A consumed or revoked token of the same family was presented again.</summary>
    ReuseDetected = 3
}
