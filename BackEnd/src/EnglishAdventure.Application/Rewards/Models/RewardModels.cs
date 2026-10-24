using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Domain.Entities.Rewards;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Services;

namespace EnglishAdventure.Application.Rewards.Models;

/// <summary>
/// One badge as a child or Parent sees it (S5.3): the definition's texts, with a level badge's {level} replaced by the
/// level's island name (its description, else its title), and whether and when the child won it.
/// </summary>
/// <param name="Kind">"FirstLesson", "Perfect", "LevelConqueror" or "LevelSuperstar".</param>
/// <param name="LevelId">A level badge's level; null for the others.</param>
public sealed record BadgeView(
    string Kind,
    Guid? LevelId,
    string Name,
    string Description,
    string? ImageUrl,
    bool Earned,
    DateTime? AwardedAtUtc);

/// <summary>A child's points and every badge they can win on the published map, won or not (S5.3).</summary>
public sealed record RewardsOverview(int Points, IReadOnlyList<BadgeView> Badges);

/// <summary>What finishing an attempt earned (S5.1, S5.2): points for the new stars, and the new badges.</summary>
public sealed record EarnedRewards(int Points, IReadOnlyList<BadgeView> Badges);

/// <summary>A badge's texts and picture for the Admin (S5.2). {level} in a level badge's texts names the level.</summary>
public sealed record BadgeDefinitionView(string Kind, bool PerLevel, string Name, string Description, string? ImageUrl);

public static class BadgeViews
{
    /// <summary>The island name of a level, as badges show it.</summary>
    public static string LevelName(MapLevel level) => level.Description ?? level.Title;

    public static BadgeView For(
        BadgeDefinition definition,
        Guid? levelId,
        string? levelName,
        BadgeAward? award) =>
        new(
            definition.Kind.ToString(),
            levelId,
            BadgeDefinition.For(definition.Name, levelName),
            BadgeDefinition.For(definition.Description, levelName),
            definition.ImageUrl?.Value,
            award is not null,
            award?.AwardedAtUtc);

    /// <summary>
    /// Every badge on the published map: the two general ones, then each level's two, in the map's order. A level
    /// badge won on a level that is no longer published is still listed (rewards are never taken back).
    /// </summary>
    public static IReadOnlyList<BadgeView> Overview(
        IReadOnlyList<BadgeDefinition> definitions,
        IReadOnlyList<BadgeAward> awards,
        LearnerMap map)
    {
        var byKind = definitions.ToDictionary(definition => definition.Kind);
        BadgeAward? Award(BadgeKind kind, Guid? levelId) =>
            awards.FirstOrDefault(award => award.Kind == kind && award.LevelId == levelId);

        var views = new List<BadgeView>();
        foreach (var kind in new[] { BadgeKind.FirstLesson, BadgeKind.Perfect })
        {
            if (byKind.TryGetValue(kind, out var definition))
            {
                views.Add(For(definition, null, null, Award(kind, null)));
            }
        }

        foreach (var level in map.Levels)
        {
            foreach (var kind in new[] { BadgeKind.LevelConqueror, BadgeKind.LevelSuperstar })
            {
                if (byKind.TryGetValue(kind, out var definition))
                {
                    views.Add(For(definition, level.Id, LevelName(level), Award(kind, level.Id)));
                }
            }
        }

        var listed = map.Levels.Select(level => level.Id).ToHashSet();
        foreach (var award in awards.Where(award => award.LevelId is { } id && !listed.Contains(id)))
        {
            if (byKind.TryGetValue(award.Kind, out var definition))
            {
                views.Add(For(definition, award.LevelId, levelName: null, award));
            }
        }

        return views;
    }

    public static BadgeDefinitionView Definition(BadgeDefinition definition) =>
        new(definition.Kind.ToString(), definition.PerLevel, definition.Name, definition.Description, definition.ImageUrl?.Value);
}

/// <summary>Turns earned badges into what the child is shown.</summary>
public static class EarnedViews
{
    public static EarnedRewards From(
        RewardsEarned earned,
        IReadOnlyList<BadgeDefinition> definitions,
        IReadOnlyList<BadgeAward> awards,
        LearnerMap map)
    {
        var badges = earned.Badges
            .Select(win =>
            {
                var definition = definitions.Single(candidate => candidate.Kind == win.Kind);
                var level = map.Levels.FirstOrDefault(candidate => candidate.Id == win.LevelId);
                var award = awards.Single(candidate => candidate.Kind == win.Kind && candidate.LevelId == win.LevelId);
                return BadgeViews.For(definition, win.LevelId, level is null ? null : BadgeViews.LevelName(level), award);
            })
            .ToList();
        return new EarnedRewards(earned.Points, badges);
    }
}
