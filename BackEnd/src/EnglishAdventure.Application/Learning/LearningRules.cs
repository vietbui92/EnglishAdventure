using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Rewards;
using EnglishAdventure.Application.Rewards.Models;
using EnglishAdventure.Domain.Entities.Rewards;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.Services;

namespace EnglishAdventure.Application.Learning;

/// <summary>Shared checks of the Learning use cases.</summary>
internal static class LearningRules
{
    /// <summary>
    /// The child of the current child session, still active (PD-010). The profile comes from the session, never from
    /// the request.
    /// </summary>
    public static async Task<Guid> CurrentChildAsync(
        IUserContext userContext,
        IChildProfileRepository profiles,
        CancellationToken cancellationToken)
    {
        var parentId = userContext.UserId ?? throw new UnauthorizedException();
        var profileId = userContext.ChildProfileId ?? throw new UnauthorizedException();
        var profile = await profiles.GetOwnedAsync(profileId, parentId, cancellationToken)
                      ?? throw new UnauthorizedException();
        profile.EnsureCanLearn();
        return profile.Id;
    }

    /// <summary>The child's attempt; another child's attempt is reported as not found.</summary>
    public static async Task<LessonAttempt> GetOwnAttemptAsync(
        ILessonAttemptRepository attempts,
        Guid attemptId,
        Guid childProfileId,
        CancellationToken cancellationToken)
    {
        var attempt = await attempts.GetAsync(attemptId, cancellationToken);
        return attempt is not null && attempt.IsFor(childProfileId)
            ? attempt
            : throw new NotFoundException("LessonAttempt", attemptId);
    }

    /// <summary>A lesson or boss children can play: published, in a published level (S3.4); otherwise not found.</summary>
    public static async Task<Lesson> GetPlayableLessonAsync(
        ILessonRepository lessons,
        ILevelRepository levels,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var lesson = await lessons.GetAsync(lessonId, cancellationToken);
        if (lesson is null || lesson.Status != ContentStatus.Published)
        {
            throw new NotFoundException("Lesson", lessonId);
        }

        var level = await levels.GetAsync(lesson.LevelId, cancellationToken);
        return level?.Status == ContentStatus.Published ? lesson : throw new NotFoundException("Lesson", lessonId);
    }

    /// <summary>The attempt's lesson, as it is now (a published lesson's structure is locked, PD-012).</summary>
    public static async Task<Lesson> GetLessonOfAsync(
        ILessonRepository lessons,
        LessonAttempt attempt,
        CancellationToken cancellationToken) =>
        await lessons.GetAsync(attempt.LessonId, cancellationToken)
        ?? throw new NotFoundException("Lesson", attempt.LessonId);

    /// <summary>The published map and where the child is on it (S4.2).</summary>
    public static async Task<(LearnerMap Map, AdventureProgress Progress)> ProgressAsync(
        IPlayQueries queries,
        ILessonCompletionRepository completions,
        Guid childProfileId,
        CancellationToken cancellationToken)
    {
        var map = await queries.GetMapAsync(cancellationToken);
        var stars = await completions.GetBestStarsAsync(childProfileId, cancellationToken);
        var paths = map.Levels
            .Select(level => new LevelPath(level.Id, level.Lessons.Select(lesson => lesson.Id).ToList(), level.Boss?.Id))
            .ToList();
        return (map, AdventureProgress.Compute(paths, stars));
    }

    /// <summary>
    /// Refuses a lesson that is not open for the child (422 <c>lesson-locked</c>, S4.2). An unfinished attempt can
    /// always be played to its end (owner 2026-10-24), even if the lesson closed since.
    /// </summary>
    public static async Task EnsureOpenAsync(
        IPlayQueries queries,
        ILessonCompletionRepository completions,
        ILessonAttemptRepository attempts,
        Guid childProfileId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var (_, progress) = await ProgressAsync(queries, completions, childProfileId, cancellationToken);
        if (progress.Find(lessonId)?.CanStart == true
            || await attempts.GetInProgressAsync(childProfileId, lessonId, cancellationToken) is not null)
        {
            return;
        }

        throw new DomainException("This lesson is not open yet.", LearningErrorCodes.LessonLocked);
    }

    /// <summary>
    /// When an answer finishes the attempt with a star: completes the lesson or raises its best result (S4.2), and pays
    /// what the better result earns (S5.1, S5.2): 10 points per new star and the badges now won. All of it is saved
    /// with that answer; unique indexes stop two finishes at once (409, and the answer is sent again), so nothing is
    /// ever paid twice. Returns what was earned, or null when nothing was.
    /// </summary>
    public static async Task<EarnedRewards?> FinishAsync(
        ILessonCompletionRepository completions,
        IRewardsRepository rewards,
        IPlayQueries queries,
        LessonAttempt attempt,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!LessonCompletion.Completes(attempt))
        {
            return null;
        }

        var childId = attempt.ChildProfileId;
        var completion = await completions.GetAsync(childId, attempt.LessonId, cancellationToken);
        var previousStars = completion?.BestStars ?? 0;
        if (completion is null)
        {
            completions.Add(LessonCompletion.From(attempt));
        }
        else if (!completion.Improve(attempt))
        {
            return null;
        }

        // The saved best stars, with this result in them.
        var stars = new Dictionary<Guid, int>(await completions.GetBestStarsAsync(childId, cancellationToken))
        {
            [attempt.LessonId] = Math.Max(previousStars, attempt.Stars!.Value),
        };
        var map = await queries.GetMapAsync(cancellationToken);
        var paths = map.Levels
            .Select(level => new LevelPath(level.Id, level.Lessons.Select(lesson => lesson.Id).ToList(), level.Boss?.Id))
            .ToList();
        var awards = (await rewards.GetAwardsAsync(childId, cancellationToken)).ToList();
        var earned = RewardRules.For(attempt.LessonId, previousStars, paths, stars, OwnedBadges.Of(awards));
        if (earned.NewStars.Count == 0 && earned.Badges.Count == 0)
        {
            return null;
        }

        foreach (var star in earned.NewStars)
        {
            rewards.Add(PointsTransaction.ForStar(childId, attempt.LessonId, star, nowUtc));
        }

        foreach (var badge in earned.Badges)
        {
            var award = BadgeAward.Win(childId, badge.Kind, badge.LevelId, attempt.LessonId, nowUtc);
            rewards.Add(award);
            awards.Add(award);
        }

        return EarnedViews.From(earned, await rewards.GetDefinitionsAsync(cancellationToken), awards, map);
    }
}
