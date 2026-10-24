using EnglishAdventure.Domain.Entities.Learning;

namespace EnglishAdventure.Application.Learning;

public interface ILessonAttemptRepository
{
    /// <summary>The attempt with its answers.</summary>
    Task<LessonAttempt?> GetAsync(Guid attemptId, CancellationToken cancellationToken);

    /// <summary>The child's unfinished attempt of the lesson, with its answers (at most one, S4.1).</summary>
    Task<LessonAttempt?> GetInProgressAsync(Guid childProfileId, Guid lessonId, CancellationToken cancellationToken);

    void Add(LessonAttempt attempt);
}
