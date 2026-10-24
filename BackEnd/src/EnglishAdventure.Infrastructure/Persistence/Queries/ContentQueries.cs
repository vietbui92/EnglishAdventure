using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Infrastructure.Persistence.Configurations.Content;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Queries;

internal sealed class ContentQueries : IContentQueries
{
    private readonly EnglishAdventureDbContext _dbContext;

    public ContentQueries(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LevelSummary>> ListLevelsAsync(
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var levels = await LevelSummaries(
                _dbContext.Levels.Where(level => includeArchived || level.Status != ContentStatus.Archived))
            .ToListAsync(cancellationToken);

        return InOrder(levels, level => level.Status, level => level.SortOrder, level => level.ArchivedAtUtc);
    }

    public async Task<LevelDetails?> GetLevelAsync(Guid levelId, CancellationToken cancellationToken)
    {
        var level = await LevelSummaries(_dbContext.Levels.Where(candidate => candidate.Id == levelId))
            .SingleOrDefaultAsync(cancellationToken);
        if (level is null)
        {
            return null;
        }

        var lessons = await LessonSummaries(_dbContext.Lessons.Where(lesson => lesson.LevelId == levelId))
            .ToListAsync(cancellationToken);

        return new LevelDetails(
            level,
            InOrder(lessons, lesson => lesson.Status, lesson => lesson.SortOrder, lesson => lesson.ArchivedAtUtc));
    }

    public Task<LessonSummary?> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken) =>
        LessonSummaries(_dbContext.Lessons.Where(lesson => lesson.Id == lessonId))
            .SingleOrDefaultAsync(cancellationToken);

    private IQueryable<LevelSummary> LevelSummaries(IQueryable<Level> levels) =>
        levels.AsNoTracking().Select(level => new LevelSummary(
            level.Id,
            level.Title.Value,
            level.Description,
            level.SortOrder,
            level.Status.ToString(),
            level.CreatedAtUtc,
            level.PublishedAtUtc,
            level.ArchivedAtUtc,
            new LessonCounts(
                _dbContext.Lessons.Count(lesson => lesson.LevelId == level.Id && lesson.Status == ContentStatus.Draft),
                _dbContext.Lessons.Count(lesson => lesson.LevelId == level.Id && lesson.Status == ContentStatus.Published),
                _dbContext.Lessons.Count(lesson => lesson.LevelId == level.Id && lesson.Status == ContentStatus.Archived)),
            ContentVersion.Encode(EF.Property<byte[]>(level, OrderedContentMapping.RowVersion))));

    private static IQueryable<LessonSummary> LessonSummaries(IQueryable<Lesson> lessons) =>
        lessons.AsNoTracking().Select(lesson => new LessonSummary(
            lesson.Id,
            lesson.LevelId,
            lesson.Title.Value,
            lesson.SortOrder,
            lesson.Status.ToString(),
            lesson.CreatedAtUtc,
            lesson.PublishedAtUtc,
            lesson.ArchivedAtUtc,
            ContentVersion.Encode(EF.Property<byte[]>(lesson, OrderedContentMapping.RowVersion))));

    /// <summary>Non-archived items by position, then archived ones by when they were archived.</summary>
    private static List<T> InOrder<T>(
        IEnumerable<T> items,
        Func<T, string> status,
        Func<T, int> sortOrder,
        Func<T, DateTime?> archivedAtUtc) =>
        items
            .OrderBy(item => status(item) == nameof(ContentStatus.Archived))
            .ThenBy(item => status(item) == nameof(ContentStatus.Archived) ? 0 : sortOrder(item))
            .ThenBy(archivedAtUtc)
            .ToList();
}
