using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Rewards;

/// <summary>
/// How a badge looks to children (S5.2): one per <see cref="BadgeKind"/>, whose rule is fixed in code. The Admin edits
/// its name, description and picture (owner 2026-10-24); a level badge's texts may say <see cref="LevelPlaceholder"/>,
/// which is replaced by the level's island name. Without a picture the app draws one.
/// </summary>
public sealed class BadgeDefinition : Entity
{
    public const int NameMaxLength = 60;
    public const int DescriptionMaxLength = 200;
    public const string LevelPlaceholder = "{level}";

    private BadgeDefinition()
    {
    }

    public BadgeKind Kind { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ImageUrl? ImageUrl { get; private set; }

    /// <summary>The badge is won once per level, and its texts may name the level.</summary>
    public bool PerLevel => IsPerLevel(Kind);

    public static bool IsPerLevel(BadgeKind kind) => kind is BadgeKind.LevelConqueror or BadgeKind.LevelSuperstar;

    public static BadgeDefinition Create(BadgeKind kind, string name, string description, ImageUrl? imageUrl)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("Unknown badge.");
        }

        var badge = new BadgeDefinition { Id = Guid.NewGuid(), Kind = kind };
        badge.Change(name, description, imageUrl);
        return badge;
    }

    public void Change(string name, string description, ImageUrl? imageUrl)
    {
        Name = ValidText(name, NameMaxLength, "A name");
        Description = ValidText(description, DescriptionMaxLength, "A description");
        ImageUrl = imageUrl;
    }

    /// <summary>The name or description as a child sees it, for the given level when the badge is per level.</summary>
    public static string For(string text, string? levelName) =>
        levelName is null ? text : text.Replace(LevelPlaceholder, levelName, StringComparison.Ordinal);

    private static string ValidText(string? value, int maxLength, string what)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length > 0 && trimmed.Length <= maxLength
            ? trimmed
            : throw new DomainException($"{what} of 1 to {maxLength} characters is required.");
    }
}
