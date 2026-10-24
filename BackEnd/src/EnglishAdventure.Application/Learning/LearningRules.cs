using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Family;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.Enums;

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
}
