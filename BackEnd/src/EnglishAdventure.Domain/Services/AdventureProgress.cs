using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Domain.Services;

/// <summary>A published level as children walk it: its lessons in order, then its boss.</summary>
public sealed record LevelPath(Guid LevelId, IReadOnlyList<Guid> LessonIds, Guid? BossId);

/// <param name="BestStars">0 when not completed.</param>
public sealed record StopProgress(Guid LessonId, LessonState State, int BestStars)
{
    /// <summary>Whether the child may start it. A completed lesson is always open; a closed-again boss is not.</summary>
    public bool CanStart => State != LessonState.Locked;
}

/// <param name="Unlocked">The child may enter the level: the first one, or the previous boss was defeated.</param>
/// <param name="Beaten">The level's boss was defeated (at least one star).</param>
public sealed record LevelProgress(
    Guid LevelId,
    bool Unlocked,
    bool Beaten,
    IReadOnlyList<StopProgress> Lessons,
    StopProgress? Boss);

/// <summary>
/// A child's way through the adventure (S4.2, PD-002, PD-011), computed from the published content in order and the
/// child's best stars; nothing of it is stored. Owner decisions 2026-10-24:
/// <list type="bullet">
/// <item>The first lesson of the first level is open. A lesson opens when the lesson before it has a star.</item>
/// <item>A completed lesson stays open, even when the Admin later puts a new lesson before it.</item>
/// <item>The boss opens when every lesson of its level has a star, so a new lesson closes it again until then.</item>
/// <item>Defeating the boss (one star or more) opens the next level and its first lesson, for good.</item>
/// </list>
/// </summary>
public sealed class AdventureProgress
{
    private AdventureProgress(IReadOnlyList<LevelProgress> levels)
    {
        Levels = levels;
    }

    public IReadOnlyList<LevelProgress> Levels { get; }

    /// <summary>The child's level: the number (from 1) of the last level they may enter; 0 without levels.</summary>
    public int CurrentLevel
    {
        get
        {
            var current = 0;
            for (var index = 0; index < Levels.Count; index++)
            {
                if (Levels[index].Unlocked)
                {
                    current = index + 1;
                }
            }

            return current;
        }
    }

    public static AdventureProgress Compute(IReadOnlyList<LevelPath> levels, IReadOnlyDictionary<Guid, int> bestStars)
    {
        var result = new List<LevelProgress>();
        var unlocked = true;
        foreach (var level in levels)
        {
            var lessons = new List<StopProgress>();
            var previousDone = true;
            var allDone = true;
            foreach (var lessonId in level.LessonIds)
            {
                var stars = bestStars.GetValueOrDefault(lessonId);
                var done = stars >= 1;
                var state = done
                    ? LessonState.Completed
                    : unlocked && previousDone ? LessonState.Available : LessonState.Locked;
                lessons.Add(new StopProgress(lessonId, state, stars));
                previousDone = done;
                allDone &= done;
            }

            StopProgress? boss = null;
            var beaten = allDone;
            if (level.BossId is { } bossId)
            {
                var stars = bestStars.GetValueOrDefault(bossId);
                // A defeated boss shows its stars but closes again while a lesson of its level has none.
                var state = !(unlocked && allDone)
                    ? LessonState.Locked
                    : stars >= 1 ? LessonState.Completed : LessonState.Available;
                boss = new StopProgress(bossId, state, stars);
                beaten = stars >= 1;
            }

            result.Add(new LevelProgress(level.LevelId, unlocked, beaten, lessons, boss));
            // Once defeated, a boss keeps the next level open.
            unlocked = beaten;
        }

        return new AdventureProgress(result);
    }

    /// <summary>The lesson's or boss's progress; null when it is not on the published map.</summary>
    public StopProgress? Find(Guid lessonId) =>
        Levels.SelectMany(level => level.Boss is null ? level.Lessons : level.Lessons.Append(level.Boss))
            .SingleOrDefault(stop => stop.LessonId == lessonId);
}
