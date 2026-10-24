using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Rewards;

public static class BadgeKinds
{
    /// <summary>The badge's name exactly as listed; numbers are not accepted.</summary>
    public static bool TryParse(string? value, out BadgeKind kind)
    {
        kind = default;
        return !string.IsNullOrEmpty(value)
               && Enum.GetNames<BadgeKind>().Contains(value, StringComparer.Ordinal)
               && Enum.TryParse(value, out kind);
    }

    public static BadgeKind Parse(string value) =>
        TryParse(value, out var kind) ? kind : throw new FormatException("Invalid badge.");
}
