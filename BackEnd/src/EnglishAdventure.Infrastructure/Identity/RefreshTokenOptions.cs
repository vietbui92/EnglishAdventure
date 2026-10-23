namespace EnglishAdventure.Infrastructure.Identity;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Authentication:RefreshToken";

    /// <summary>
    /// Lifetime of each refresh token. Every rotation issues a new token with a fresh lifetime (sliding session).
    /// </summary>
    public int LifetimeDays { get; set; } = 14;
}
