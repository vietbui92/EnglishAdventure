using EnglishAdventure.Domain.Entities.Content;

namespace EnglishAdventure.Application.Content;

public interface ILessonRepository
{
    Task<Lesson?> GetAsync(Guid lessonId, CancellationToken cancellationToken);

    /// <summary>The level's non-archived lessons in their order.</summary>
    Task<IReadOnlyList<Lesson>> ListInOrderAsync(Guid levelId, CancellationToken cancellationToken);

    /// <summary>The highest position among the level's non-archived lessons, 0 when there are none.</summary>
    Task<int> MaxSortOrderAsync(Guid levelId, CancellationToken cancellationToken);

    void Add(Lesson lesson);

    /// <summary>
    /// Saving fails with a conflict unless the lesson is still at <paramref name="version"/>, the version the client
    /// last read (optimistic concurrency).
    /// </summary>
    void ExpectVersion(Lesson lesson, byte[] version);
}
