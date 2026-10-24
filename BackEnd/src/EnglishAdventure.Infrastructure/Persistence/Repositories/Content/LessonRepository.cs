using EnglishAdventure.Application.Content;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Infrastructure.Persistence.Configurations.Content;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Content;

internal sealed class LessonRepository : ILessonRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public LessonRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Lesson?> GetAsync(Guid lessonId, CancellationToken cancellationToken) =>
        _dbContext.Lessons.SingleOrDefaultAsync(lesson => lesson.Id == lessonId, cancellationToken);

    public async Task<IReadOnlyList<Lesson>> ListInOrderAsync(Guid levelId, CancellationToken cancellationToken) =>
        await _dbContext.Lessons
            .Where(lesson => lesson.LevelId == levelId && lesson.Status != ContentStatus.Archived)
            .OrderBy(lesson => lesson.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<int> MaxSortOrderAsync(Guid levelId, CancellationToken cancellationToken) =>
        await _dbContext.Lessons
            .Where(lesson => lesson.LevelId == levelId && lesson.Status != ContentStatus.Archived)
            .MaxAsync(lesson => (int?)lesson.SortOrder, cancellationToken) ?? 0;

    public void Add(Lesson lesson) => _dbContext.Lessons.Add(lesson);

    public void ExpectVersion(Lesson lesson, byte[] version) =>
        _dbContext.Entry(lesson).Property<byte[]>(OrderedContentMapping.RowVersion).OriginalValue = version;
}
