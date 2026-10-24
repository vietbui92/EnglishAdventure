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
        levels = await WithBossStatusAsync(levels, cancellationToken);

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

        // Lessons in their order, then the boss, then everything archived.
        var ordered = lessons
            .OrderBy(lesson => lesson.Status == nameof(ContentStatus.Archived))
            .ThenBy(lesson => lesson.Kind == nameof(LessonKind.Boss))
            .ThenBy(lesson => lesson.Status == nameof(ContentStatus.Archived) ? 0 : lesson.SortOrder)
            .ThenBy(lesson => lesson.ArchivedAtUtc)
            .ToList();

        return new LevelDetails((await WithBossStatusAsync([level], cancellationToken))[0], ordered);
    }

    public Task<LessonSummary?> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken) =>
        LessonSummaries(_dbContext.Lessons.Where(lesson => lesson.Id == lessonId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<LessonWordBank?> GetLessonWordBankAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .Where(candidate => candidate.Id == lessonId)
            .Select(candidate => new
            {
                candidate.Status,
                candidate.Kind,
                candidate.LevelId,
                Version = EF.Property<byte[]>(candidate, OrderedContentMapping.RowVersion),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        // A boss has no words of its own: it shows the words of its level's lessons, which it may use (S3.8).
        var boss = lesson.Kind == LessonKind.Boss;
        var words = boss
            ? await (from word in _dbContext.Set<LessonWord>().AsNoTracking()
                     join owner in _dbContext.Lessons on word.LessonId equals owner.Id
                     where owner.LevelId == lesson.LevelId
                           && owner.Kind == LessonKind.Regular
                           && owner.Status != ContentStatus.Archived
                     orderby owner.SortOrder, word.SortOrder
                     select word)
                .ToListAsync(cancellationToken)
            : await _dbContext.Set<LessonWord>()
                .AsNoTracking()
                .Where(word => word.LessonId == lessonId)
                .OrderBy(word => word.SortOrder)
                .ToListAsync(cancellationToken);

        return new LessonWordBank(
            lessonId,
            lesson.Status.ToString(),
            ContentVersion.Encode(lesson.Version),
            boss ? 0 : Lesson.MaxWords,
            words.Select(word => new LessonWordSummary(
                    word.Id,
                    word.SortOrder,
                    word.English,
                    word.Meaning,
                    word.ImageUrl?.Value,
                    word.ShowInLearnStep))
                .ToList());
    }

    public async Task<LessonActivities?> GetLessonActivitiesAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .Where(candidate => candidate.Id == lessonId)
            .Select(candidate => new
            {
                candidate.Status,
                Version = EF.Property<byte[]>(candidate, OrderedContentMapping.RowVersion),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        var activities = await _dbContext.Set<LessonActivity>()
            .AsNoTracking()
            .Where(activity => activity.LessonId == lessonId)
            .Include(activity => ((QuizActivity)activity).Questions)
            .ThenInclude(question => question.Options)
            .Include(activity => ((MatchingActivity)activity).Pairs)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new LessonActivities(
            lessonId,
            lesson.Status.ToString(),
            ContentVersion.Encode(lesson.Version),
            Lesson.MaxActivities,
            QuizActivity.MaxQuestions,
            activities
                .OrderBy(activity => activity.SortOrder)
                .Select(activity => new ActivitySummary(
                    activity.Id,
                    activity.Type.ToString(),
                    activity.SortOrder,
                    activity is QuizActivity quiz ? quiz.Questions.Select(ToSummary).ToList() : [],
                    (activity as MatchingActivity)?.Mode.ToString(),
                    activity is MatchingActivity matching ? matching.Pairs.Select(pair => pair.WordId).ToList() : []))
                .ToList());
    }

    private static QuizQuestionSummary ToSummary(QuizQuestion question) =>
        new(
            question.Id,
            question.SortOrder,
            question.Form.ToString(),
            question.CorrectWordId,
            question.Options
                .Where(option => !question.IsCorrect(option))
                .Select(option => option.WordId)
                .ToList(),
            question.Sentence);

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
                _dbContext.Lessons.Count(lesson => lesson.LevelId == level.Id && lesson.Kind == LessonKind.Regular && lesson.Status == ContentStatus.Draft),
                _dbContext.Lessons.Count(lesson => lesson.LevelId == level.Id && lesson.Kind == LessonKind.Regular && lesson.Status == ContentStatus.Published),
                _dbContext.Lessons.Count(lesson => lesson.LevelId == level.Id && lesson.Kind == LessonKind.Regular && lesson.Status == ContentStatus.Archived)),
            ContentVersion.Encode(EF.Property<byte[]>(level, OrderedContentMapping.RowVersion)),
            null));

    /// <summary>Adds the status of each level's boss that is not archived (S3.8).</summary>
    private async Task<List<LevelSummary>> WithBossStatusAsync(List<LevelSummary> levels, CancellationToken cancellationToken)
    {
        var levelIds = levels.Select(level => level.Id).ToList();
        var bosses = await _dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => levelIds.Contains(lesson.LevelId)
                             && lesson.Kind == LessonKind.Boss
                             && lesson.Status != ContentStatus.Archived)
            .Select(lesson => new { lesson.LevelId, lesson.Status })
            .ToListAsync(cancellationToken);
        var statusByLevel = bosses.ToDictionary(boss => boss.LevelId, boss => boss.Status.ToString());

        return levels
            .Select(level => level with { BossStatus = statusByLevel.GetValueOrDefault(level.Id) })
            .ToList();
    }

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
            ContentVersion.Encode(EF.Property<byte[]>(lesson, OrderedContentMapping.RowVersion)),
            lesson.Kind.ToString(),
            lesson.BossImageUrl != null ? lesson.BossImageUrl.Value : null));

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
