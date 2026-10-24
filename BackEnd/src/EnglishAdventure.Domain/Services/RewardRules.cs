using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Domain.Services;

/// <summary>A badge to award: its kind, and its level for a level badge.</summary>
public sealed record BadgeWin(BadgeKind Kind, Guid? LevelId);

/// <summary>What one improved result earns: the numbers of the newly earned stars, and new badges.</summary>
public sealed record RewardsEarned(IReadOnlyList<int> NewStars, IReadOnlyList<BadgeWin> Badges)
{
    public int Points => NewStars.Count * Entities.Rewards.PointsTransaction.PointsPerStar;
}

/// <summary>
/// The rewards of PD-011 (S5.1, S5.2), decided when a lesson's best result rises: 10 points for each newly earned star
/// (each star of a lesson once), and the badges whose condition is now met and the child does not have yet. Rewards
/// are never taken back (owner 2026-10-24).
/// </summary>
public static class RewardRules
{
    /// <param name="lessonId">The lesson or boss whose best result changed.</param>
    /// <param name="previousStars">Its best stars before (0 when it was not completed).</param>
    /// <param name="map">The published levels in order (S4.2).</param>
    /// <param name="bestStars">The child's best stars per lesson, already including the new result.</param>
    /// <param name="owned">The badges the child has.</param>
    public static RewardsEarned For(
        Guid lessonId,
        int previousStars,
        IReadOnlyList<LevelPath> map,
        IReadOnlyDictionary<Guid, int> bestStars,
        IReadOnlySet<BadgeWin> owned)
    {
        var stars = bestStars.GetValueOrDefault(lessonId);
        var newStars = Enumerable.Range(previousStars + 1, Math.Max(0, stars - previousStars)).ToList();

        var badges = new List<BadgeWin>();
        void Win(BadgeKind kind, Guid? levelId)
        {
            var badge = new BadgeWin(kind, levelId);
            if (!owned.Contains(badge))
            {
                badges.Add(badge);
            }
        }

        if (stars >= 1)
        {
            Win(BadgeKind.FirstLesson, null);
        }

        if (stars == 3)
        {
            Win(BadgeKind.Perfect, null);
        }

        var level = map.FirstOrDefault(path => path.BossId == lessonId || path.LessonIds.Contains(lessonId));
        if (level is not null)
        {
            if (level.BossId == lessonId && stars >= 1)
            {
                Win(BadgeKind.LevelConqueror, level.LevelId);
            }

            var all = level.BossId is { } bossId ? level.LessonIds.Append(bossId) : level.LessonIds;
            if (level.BossId is not null && all.All(id => bestStars.GetValueOrDefault(id) == 3))
            {
                Win(BadgeKind.LevelSuperstar, level.LevelId);
            }
        }

        return new RewardsEarned(newStars, badges);
    }
}
