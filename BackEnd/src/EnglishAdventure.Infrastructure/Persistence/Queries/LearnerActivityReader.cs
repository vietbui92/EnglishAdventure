using EnglishAdventure.Application.Content;

namespace EnglishAdventure.Infrastructure.Persistence.Queries;

/// <summary>
/// No learning data exists before lesson attempts (S4.1), so no content has been started yet. S4.1 replaces this with
/// a query over the attempts table.
/// </summary>
internal sealed class LearnerActivityReader : ILearnerActivityReader
{
    public Task<bool> HasLessonBeenStartedAsync(Guid lessonId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> HasAnyLessonOfLevelBeenStartedAsync(Guid levelId, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
