using EnglishAdventure.Application.Content;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Queries;

/// <summary>
/// The learner side of the content (S3.4). Only published items of published levels are read. The answers stay here:
/// a question's options use the options' own identities and are shuffled on every read, and a matching's right side
/// uses <see cref="MatchingPair.ChoiceId"/> and its own shuffle, so nothing sent links an option or a partner to its word.
/// </summary>
internal sealed class PlayQueries : IPlayQueries
{
    private readonly EnglishAdventureDbContext _dbContext;

    public PlayQueries(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LearnerMap> GetMapAsync(CancellationToken cancellationToken)
    {
        var levels = await _dbContext.Levels
            .AsNoTracking()
            .Where(level => level.Status == ContentStatus.Published)
            .OrderBy(level => level.SortOrder)
            .ToListAsync(cancellationToken);
        var levelIds = levels.Select(level => level.Id).ToList();
        var lessons = await _dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => levelIds.Contains(lesson.LevelId) && lesson.Status == ContentStatus.Published)
            .ToListAsync(cancellationToken);

        return new LearnerMap(levels
            .Select(level =>
            {
                var own = lessons.Where(lesson => lesson.LevelId == level.Id).ToList();
                var boss = own.FirstOrDefault(lesson => lesson.IsBoss);
                return new MapLevel(
                    level.Id,
                    level.Title.Value,
                    level.Description,
                    own.Where(lesson => !lesson.IsBoss)
                        .OrderBy(lesson => lesson.SortOrder)
                        .Select(lesson => new MapLesson(lesson.Id, lesson.Title.Value))
                        .ToList(),
                    boss is null ? null : new MapBoss(boss.Id, boss.Title.Value, boss.BossImageUrl?.Value));
            })
            // A level appears once it has something to play (the domain model proposal, S3.1).
            .Where(level => level.Lessons.Count > 0)
            .ToList());
    }

    public async Task<PlayLesson?> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .Include(candidate => candidate.Words)
            .Include(candidate => candidate.Activities)
            .ThenInclude(activity => ((QuizActivity)activity).Questions)
            .ThenInclude(question => question.Options)
            .Include(candidate => candidate.Activities)
            .ThenInclude(activity => ((MatchingActivity)activity).Pairs)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == lessonId
                             && candidate.Status == ContentStatus.Published
                             && _dbContext.Levels.Any(level => level.Id == candidate.LevelId
                                                               && level.Status == ContentStatus.Published),
                cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        // A boss shows words that live in the level's lessons (S3.8).
        var words = lesson.IsBoss
            ? await LoadWordsAsync(UsedWordIds(lesson), cancellationToken)
            : lesson.Words.ToDictionary(word => word.Id);

        return new PlayLesson(
            lesson.Id,
            lesson.LevelId,
            lesson.Title.Value,
            lesson.Kind.ToString(),
            lesson.BossImageUrl?.Value,
            lesson.LearnCards
                .Select(card => new LearnCard(
                    card.English,
                    card.Meaning,
                    card.ImageUrl?.Value,
                    card.Movement.ToString(),
                    card.SoundUrl?.Value))
                .ToList(),
            lesson.Activities.Select(activity => ToPlay(activity, words)).ToList());
    }

    private async Task<Dictionary<Guid, LessonWord>> LoadWordsAsync(
        IReadOnlyCollection<Guid> wordIds,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<LessonWord>()
            .AsNoTracking()
            .Where(word => wordIds.Contains(word.Id))
            .ToDictionaryAsync(word => word.Id, cancellationToken);

    private static List<Guid> UsedWordIds(Lesson lesson) =>
        lesson.Activities
            .SelectMany(activity => activity switch
            {
                QuizActivity quiz => quiz.Questions.SelectMany(question => question.Options.Select(option => option.WordId)),
                MatchingActivity matching => matching.Pairs.Select(pair => pair.WordId),
                _ => [],
            })
            .Distinct()
            .ToList();

    private static PlayActivity ToPlay(LessonActivity activity, IReadOnlyDictionary<Guid, LessonWord> words) =>
        activity switch
        {
            QuizActivity quiz => new PlayActivity(
                quiz.Id,
                quiz.Type.ToString(),
                quiz.Questions.Select(question => ToPlay(question, words)).ToList(),
                Matching: null),
            MatchingActivity matching => new PlayActivity(matching.Id, matching.Type.ToString(), [], ToPlay(matching, words)),
            _ => throw new InvalidOperationException($"Unknown activity type {activity.Type}."),
        };

    private static PlayQuestion ToPlay(QuizQuestion question, IReadOnlyDictionary<Guid, LessonWord> words)
    {
        var correct = words[question.CorrectWordId];
        var picturesAsOptions = question.Form == QuizQuestionForm.WordToPicture;
        var options = Shuffle(question.Options)
            .Select(option =>
            {
                var word = words[option.WordId];
                return picturesAsOptions
                    ? new PlayChoice(option.Id, Text: null, word.ImageUrl?.Value)
                    : new PlayChoice(option.Id, word.English, ImageUrl: null);
            })
            .ToList();

        return question.Form switch
        {
            QuizQuestionForm.PictureToWord => new PlayQuestion(question.Id, question.Form.ToString(), null, correct.ImageUrl?.Value, options),
            QuizQuestionForm.WordToPicture => new PlayQuestion(question.Id, question.Form.ToString(), correct.English, null, options),
            _ => new PlayQuestion(question.Id, question.Form.ToString(), question.Sentence, null, options),
        };
    }

    private static PlayMatching ToPlay(MatchingActivity matching, IReadOnlyDictionary<Guid, LessonWord> words)
    {
        var pictures = matching.Mode == MatchingMode.WordToPicture;
        return new PlayMatching(
            matching.Mode.ToString(),
            Shuffle(matching.Pairs).Select(pair => new PlayChoice(pair.Id, words[pair.WordId].English, null)).ToList(),
            Shuffle(matching.Pairs)
                .Select(pair => pictures
                    ? new PlayChoice(pair.ChoiceId, Text: null, words[pair.WordId].ImageUrl?.Value)
                    : new PlayChoice(pair.ChoiceId, words[pair.WordId].Meaning, ImageUrl: null))
                .ToList());
    }

    /// <summary>A new order every time: the stored order puts the correct option first.</summary>
    private static List<T> Shuffle<T>(IEnumerable<T> items)
    {
        var list = items.ToList();
        Random.Shared.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list));
        return list;
    }
}
