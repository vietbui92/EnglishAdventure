using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace EnglishAdventure.Infrastructure.Identity;

internal static class RefreshTokenHasher
{
    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 512 bits from the OS CSPRNG, base64url-encoded so the token is safe in URLs, headers and JSON.
    /// </summary>
    public static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64Url.EncodeToString(bytes);
    }
}
