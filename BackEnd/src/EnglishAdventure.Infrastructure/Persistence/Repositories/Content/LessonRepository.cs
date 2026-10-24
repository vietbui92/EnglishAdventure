using EnglishAdventure.Application.Content;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;
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
        _dbContext.Lessons
            .Include(lesson => lesson.Words)
            .Include(lesson => lesson.Activities)
            .ThenInclude(activity => ((QuizActivity)activity).Questions)
            .ThenInclude(question => question.Options)
            .Include(lesson => lesson.Activities)
            .ThenInclude(activity => ((MatchingActivity)activity).Pairs)
            .AsSplitQuery()
            .SingleOrDefaultAsync(lesson => lesson.Id == lessonId, cancellationToken);

    public async Task<IReadOnlyList<Lesson>> ListInOrderAsync(Guid levelId, CancellationToken cancellationToken) =>
        await RegularLessons(levelId)
            .OrderBy(lesson => lesson.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<int> MaxSortOrderAsync(Guid levelId, CancellationToken cancellationToken) =>
        await RegularLessons(levelId).MaxAsync(lesson => (int?)lesson.SortOrder, cancellationToken) ?? 0;

    public Task<bool> HasBossAsync(Guid levelId, CancellationToken cancellationToken) =>
        Bosses(levelId).AnyAsync(cancellationToken);

    public Task<bool> HasPublishedBossAsync(Guid levelId, CancellationToken cancellationToken) =>
        Bosses(levelId).AnyAsync(lesson => lesson.Status == ContentStatus.Published, cancellationToken);

    public async Task<LevelWords> GetLevelWordsAsync(Guid levelId, CancellationToken cancellationToken)
    {
        var words = await (from word in _dbContext.Set<LessonWord>()
                           join lesson in RegularLessons(levelId) on word.LessonId equals lesson.Id
                           select new { word.Id, HasPicture = word.ImageUrl != null })
            .ToListAsync(cancellationToken);

        return LevelWords.From(words.Select(word => (word.Id, word.HasPicture)));
    }

    public async Task<bool> IsWordUsedByBossAsync(Guid levelId, Guid wordId, CancellationToken cancellationToken)
    {
        var bossIds = Bosses(levelId).Select(boss => boss.Id);
        var activityIds = _dbContext.Set<LessonActivity>()
            .Where(activity => bossIds.Contains(activity.LessonId))
            .Select(activity => activity.Id);
        var questionIds = _dbContext.Set<QuizQuestion>()
            .Where(question => activityIds.Contains(question.ActivityId))
            .Select(question => question.Id);

        return await _dbContext.Set<QuizOption>()
                   .AnyAsync(option => option.WordId == wordId && questionIds.Contains(option.QuestionId), cancellationToken)
               || await _dbContext.Set<MatchingPair>()
                   .AnyAsync(pair => pair.WordId == wordId && activityIds.Contains(pair.ActivityId), cancellationToken);
    }

    public void Add(Lesson lesson) => _dbContext.Lessons.Add(lesson);

    public void ExpectVersion(Lesson lesson, byte[] version) =>
        _dbContext.Entry(lesson).Property<byte[]>(OrderedContentMapping.RowVersion).OriginalValue = version;

    private IQueryable<Lesson> RegularLessons(Guid levelId) =>
        _dbContext.Lessons.Where(lesson => lesson.LevelId == levelId
                                           && lesson.Kind == LessonKind.Regular
                                           && lesson.Status != ContentStatus.Archived);

    private IQueryable<Lesson> Bosses(Guid levelId) =>
        _dbContext.Lessons.Where(lesson => lesson.LevelId == levelId
                                           && lesson.Kind == LessonKind.Boss
                                           && lesson.Status != ContentStatus.Archived);
}
