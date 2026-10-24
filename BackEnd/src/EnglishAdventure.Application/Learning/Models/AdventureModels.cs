using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Domain.Services;

namespace EnglishAdventure.Application.Learning.Models;

// The adventure map of one child (S4.2): the published levels, lessons and bosses (S3.4) with where the child is on
// each. States are "Locked", "Available" or "Completed"; best stars are 0 until completed.

/// <param name="CurrentLevel">The child's level: the number (from 1) of the last level they may enter.</param>
/// <param name="Points">The child's points (S5.1).</param>
public sealed record AdventureMap(int CurrentLevel, int Points, IReadOnlyList<AdventureLevel> Levels)
{
    public static AdventureMap From(LearnerMap map, AdventureProgress progress, int points) =>
        new(
            progress.CurrentLevel,
            points,
            map.Levels.Select((level, index) =>
                {
                    var levelProgress = progress.Levels[index];
                    return new AdventureLevel(
                        level.Id,
                        level.Title,
                        level.Description,
                        levelProgress.Unlocked,
                        levelProgress.Beaten,
                        level.Lessons.Select((lesson, position) =>
                            {
                                var stop = levelProgress.Lessons[position];
                                return new AdventureLesson(lesson.Id, lesson.Title, stop.State.ToString(), stop.BestStars);
                            })
                            .ToList(),
                        level.Boss is null || levelProgress.Boss is null
                            ? null
                            : new AdventureBoss(
                                level.Boss.Id,
                                level.Boss.Name,
                                level.Boss.ImageUrl,
                                levelProgress.Boss.State.ToString(),
                                levelProgress.Boss.BestStars));
                })
                .ToList());
}

/// <param name="Unlocked">The child may enter: the first level, or the previous boss was defeated.</param>
/// <param name="Beaten">Its boss was defeated.</param>
public sealed record AdventureLevel(
    Guid Id,
    string Title,
    string? Description,
    bool Unlocked,
    bool Beaten,
    IReadOnlyList<AdventureLesson> Lessons,
    AdventureBoss? Boss);

public sealed record AdventureLesson(Guid Id, string Title, string State, int BestStars);

public sealed record AdventureBoss(Guid Id, string Name, string? ImageUrl, string State, int BestStars);
