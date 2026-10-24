namespace EnglishAdventure.Application.Content;

/// <summary>
/// The version clients see for a level or lesson: its database row version as base64. It changes on every save.
/// </summary>
public static class ContentVersion
{
    private const int RowVersionLength = 8;

    public static string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static bool IsValid(string? version) => TryDecode(version, out _);

    /// <summary>Decodes a version that passed validation.</summary>
    public static byte[] Decode(string version) =>
        TryDecode(version, out var rowVersion) ? rowVersion : throw new FormatException("Invalid content version.");

    private static bool TryDecode(string? version, out byte[] rowVersion)
    {
        // One spare byte, so a longer value fails instead of being cut short.
        var buffer = new byte[RowVersionLength + 1];
        var valid = !string.IsNullOrWhiteSpace(version)
                    && Convert.TryFromBase64String(version, buffer, out var written)
                    && written == RowVersionLength;
        rowVersion = valid ? buffer[..RowVersionLength] : [];
        return valid;
    }
}
