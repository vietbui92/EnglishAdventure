using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Application.Content;

public interface ILessonRepository
{
    Task<Lesson?> GetAsync(Guid lessonId, CancellationToken cancellationToken);

    /// <summary>The level's non-archived regular lessons in their order (the boss is not in the order).</summary>
    Task<IReadOnlyList<Lesson>> ListInOrderAsync(Guid levelId, CancellationToken cancellationToken);

    /// <summary>The highest position among the level's non-archived regular lessons, 0 when there are none.</summary>
    Task<int> MaxSortOrderAsync(Guid levelId, CancellationToken cancellationToken);

    /// <summary>Whether the level has a boss that is not archived (S3.8: at most one).</summary>
    Task<bool> HasBossAsync(Guid levelId, CancellationToken cancellationToken);

    /// <summary>Whether the level's boss is published, which a level needs to be published (S3.8).</summary>
    Task<bool> HasPublishedBossAsync(Guid levelId, CancellationToken cancellationToken);

    /// <summary>The words of the level's non-archived regular lessons, which its boss may use (S3.8).</summary>
    Task<LevelWords> GetLevelWordsAsync(Guid levelId, CancellationToken cancellationToken);

    /// <summary>Whether the level's boss (not archived) uses the word, so the word must stay (S3.8).</summary>
    Task<bool> IsWordUsedByBossAsync(Guid levelId, Guid wordId, CancellationToken cancellationToken);

    void Add(Lesson lesson);

    /// <summary>
    /// Saving fails with a conflict unless the lesson is still at <paramref name="version"/>, the version the client
    /// last read (optimistic concurrency).
    /// </summary>
    void ExpectVersion(Lesson lesson, byte[] version);
}
