using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Application.Content;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.UnitTests.Content;

internal sealed class FakeLevelRepository : ILevelRepository
{
    public List<Level> Levels { get; } = new();

    public Task<Level?> GetAsync(Guid levelId, CancellationToken cancellationToken) =>
        Task.FromResult(Levels.SingleOrDefault(level => level.Id == levelId));

    public Task<IReadOnlyList<Level>> ListInOrderAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Level>>(
            Levels.Where(level => level.Status != ContentStatus.Archived).OrderBy(level => level.SortOrder).ToList());

    public Task<int> MaxSortOrderAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Levels.Where(level => !level.IsArchived).Select(level => level.SortOrder).DefaultIfEmpty().Max());

    public void Add(Level level) => Levels.Add(level);

    public void ExpectVersion(Level level, byte[] version)
    {
    }
}

internal sealed class FakeLessonRepository : ILessonRepository
{
    public List<Lesson> Lessons { get; } = new();

    public Task<Lesson?> GetAsync(Guid lessonId, CancellationToken cancellationToken) =>
        Task.FromResult(Lessons.SingleOrDefault(lesson => lesson.Id == lessonId));

    public Task<IReadOnlyList<Lesson>> ListInOrderAsync(Guid levelId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Lesson>>(
            Lessons.Where(lesson => lesson.LevelId == levelId && !lesson.IsArchived)
                .OrderBy(lesson => lesson.SortOrder)
                .ToList());

    public Task<int> MaxSortOrderAsync(Guid levelId, CancellationToken cancellationToken) =>
        Task.FromResult(Lessons.Where(lesson => lesson.LevelId == levelId && !lesson.IsArchived)
            .Select(lesson => lesson.SortOrder)
            .DefaultIfEmpty()
            .Max());

    public void Add(Lesson lesson) => Lessons.Add(lesson);

    public void ExpectVersion(Lesson lesson, byte[] version)
    {
    }
}

internal sealed class FakeLearnerActivityReader : ILearnerActivityReader
{
    public HashSet<Guid> StartedLessons { get; } = new();

    public HashSet<Guid> StartedLevels { get; } = new();

    public Task<bool> HasLessonBeenStartedAsync(Guid lessonId, CancellationToken cancellationToken) =>
        Task.FromResult(StartedLessons.Contains(lessonId));

    public Task<bool> HasAnyLessonOfLevelBeenStartedAsync(Guid levelId, CancellationToken cancellationToken) =>
        Task.FromResult(StartedLevels.Contains(levelId));
}

/// <summary>Counts saves and transactions; changes live in the fake repositories already.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public int Transactions { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.FromResult(0);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        Transactions++;
        await work(cancellationToken);
    }
}
