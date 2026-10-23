using Microsoft.IdentityModel.Tokens;

namespace EnglishAdventure.Infrastructure.Authentication;

/// <summary>
/// Access-token settings, bound from <c>Authentication:Jwt</c> and validated at startup by <see cref="JwtOptionsValidator"/>.
/// <see cref="SigningKey"/> is a secret: supply it through user secrets, an environment variable or Key Vault.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public const string SigningAlgorithm = SecurityAlgorithms.HmacSha256;

    public const int MinimumSigningKeyBytes = 32;

    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Base64-encoded HMAC-SHA256 key of at least 256 bits.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public SymmetricSecurityKey CreateSigningKey()
    {
        return new SymmetricSecurityKey(Convert.FromBase64String(SigningKey));
    }
}
