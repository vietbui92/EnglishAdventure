using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One playable unit of a level and the unit that is completed and unlocked (PD-002, PD-011). A regular lesson owns
/// its word bank (S3.6) and its activities (S3.2 quizzes, S3.3 matching). The level's boss challenge (S3.8) is a lesson
/// too, of kind <see cref="LessonKind.Boss"/>: a name and a picture, no word bank and no learn step, and activities that
/// point to the words of the level's lessons (handed in as <see cref="LevelWords"/>). Every change needs its level,
/// because an archived level freezes its lessons.
/// </summary>
public sealed class Lesson : OrderedContent
{
    /// <summary>Owner decision 2026-10-24 (PD-011 plans 5–8 words per lesson).</summary>
    public const int MaxWords = 12;

    /// <summary>Owner decision 2026-10-24 (PD-011 plans 2–4 activities per lesson).</summary>
    public const int MaxActivities = 4;

    private readonly List<LessonWord> _words = new();
    private readonly List<LessonActivity> _activities = new();

    private Lesson()
    {
    }

    public Guid LevelId { get; private set; }

    public LessonKind Kind { get; private set; }

    public bool IsBoss => Kind == LessonKind.Boss;

    /// <summary>The boss's picture (the boss's name is the title). Required to publish a boss; null for a lesson.</summary>
    public ImageUrl? BossImageUrl { get; private set; }

    /// <summary>The word bank in its order. Always empty for a boss.</summary>
    public IReadOnlyList<LessonWord> Words => _words.OrderBy(word => word.SortOrder).ToList();

    /// <summary>
    /// The learn step (S3.7, PD-011): the word cards a child sees before the activities, in the word bank's order.
    /// Not scored. A boss has none (owner, 2026-10-24).
    /// </summary>
    public IReadOnlyList<LessonWord> LearnCards => Words.Where(word => word.ShowInLearnStep).ToList();

    /// <summary>The games after the learn step, in their order (S3.2: quizzes; S3.3: matching).</summary>
    public IReadOnlyList<LessonActivity> Activities => _activities.OrderBy(activity => activity.SortOrder).ToList();

    /// <summary>
    /// When anything inside the lesson last changed (its words, its activities). Saving it with every such change
    /// keeps the lesson's version, and so its concurrency check, covering the whole lesson.
    /// </summary>
    public DateTime? ContentChangedAtUtc { get; private set; }

    public static Lesson Create(Level level, ContentTitle title, int sortOrder, DateTime nowUtc)
    {
        level.EnsureLessonsCanChange();

        var lesson = new Lesson { LevelId = level.Id, Kind = LessonKind.Regular };
        lesson.Initialize(title, sortOrder, nowUtc);
        return lesson;
    }

    /// <summary>
    /// Creates the level's boss challenge (S3.8). It comes after every lesson, so it stays out of the lessons' order
    /// (position 1 of its own). One boss per level is checked by the application and a unique index.
    /// </summary>
    public static Lesson CreateBoss(Level level, ContentTitle name, ImageUrl? picture, DateTime nowUtc)
    {
        level.EnsureLessonsCanChange();

        var boss = new Lesson { LevelId = level.Id, Kind = LessonKind.Boss, BossImageUrl = picture };
        boss.Initialize(name, sortOrder: 1, nowUtc);
        return boss;
    }

    public void Rename(Level level, ContentTitle title)
    {
        EnsureCanChange(level);
        ChangeTitle(title);
    }

    /// <summary>Changes the boss's name and picture, also while published; a published boss keeps a picture.</summary>
    public void UpdateBoss(Level level, ContentTitle name, ImageUrl? picture)
    {
        EnsureBoss();
        EnsureCanChange(level);
        if (Status == ContentStatus.Published && picture is null)
        {
            throw new DomainException("A published boss needs a picture.", ContentErrorCodes.LessonNotReady);
        }

        ChangeTitle(name);
        BossImageUrl = picture;
    }

    public void MoveTo(Level level, int sortOrder)
    {
        EnsureCanChange(level);
        MoveToCore(sortOrder);
    }

    /// <summary>
    /// Shows the lesson to learners. A lesson needs at least one word, every word a picture, the learn step a card, and
    /// at least one activity ready to play (owner, 2026-10-24). A boss needs its picture, an activity ready to play, and
    /// a picture for every word its activities use.
    /// </summary>
    /// <param name="levelWords">For a boss: the words of the level's lessons.</param>
    public void Publish(Level level, DateTime nowUtc, LevelWords? levelWords = null)
    {
        EnsureCanChange(level);
        PublishCore(nowUtc, () => EnsureReadyToPublish(levelWords));
    }

    /// <param name="learnersStarted">Whether a child has started this lesson.</param>
    public void Unpublish(Level level, bool learnersStarted)
    {
        EnsureCanChange(level);
        EnsureLevelKeepsBoss(level);
        UnpublishCore(learnersStarted);
    }

    /// <summary>Retires the lesson for good (PD-012). A published level keeps its boss, so its boss stays.</summary>
    public void Archive(Level level, DateTime nowUtc)
    {
        if (level.Id != LevelId)
        {
            throw new DomainException("The lesson belongs to another level.");
        }

        if (!IsArchived)
        {
            EnsureLevelKeepsBoss(level);
        }

        Archive(nowUtc);
    }

    /// <summary>Adds a word at the end of the word bank. Only while the lesson is a draft (PD-012).</summary>
    public LessonWord AddWord(Level level, string english, string meaning, ImageUrl? imageUrl, DateTime nowUtc)
    {
        EnsureHasWordBank();
        EnsureStructureCanChange(level);
        if (_words.Count >= MaxWords)
        {
            throw new DomainException($"A lesson has at most {MaxWords} words.", ContentErrorCodes.WordLimit);
        }

        EnsureNoOtherWord(english, exceptWordId: null);
        var word = LessonWord.Create(Id, _words.Count + 1, english, meaning, imageUrl);
        _words.Add(word);
        ContentChangedAtUtc = nowUtc;
        return word;
    }

    /// <summary>Corrects a word's texts and picture, also while published (PD-012 allows fixing texts and pictures).</summary>
    public void UpdateWord(Level level, Guid wordId, string english, string meaning, ImageUrl? imageUrl, DateTime nowUtc)
    {
        EnsureCanChange(level);
        EnsureNotArchived();
        var word = GetWord(wordId);
        if (Status == ContentStatus.Published && imageUrl is null)
        {
            throw new DomainException(
                "Every word of a published lesson needs a picture.",
                ContentErrorCodes.LessonNotReady);
        }

        EnsureNoOtherWord(english, word.Id);
        word.Change(english, meaning, imageUrl);
        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>
    /// Removes a word and closes the gap in the order. Only while the lesson is a draft (PD-012), and only when no
    /// activity uses it, here or in the level's boss.
    /// </summary>
    /// <param name="usedByBoss">Whether the level's boss uses the word (S3.8).</param>
    public void RemoveWord(Level level, Guid wordId, DateTime nowUtc, bool usedByBoss = false)
    {
        EnsureStructureCanChange(level);
        var word = GetWord(wordId);
        if (usedByBoss || _activities.Any(activity => activity.UsesWord(wordId)))
        {
            throw new DomainException(
                "An activity uses this word. Change or remove it there first.",
                ContentErrorCodes.WordInUse);
        }

        _words.Remove(word);
        Renumber(Words.Select(remaining => remaining.Id).ToList());
        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Puts the words in the given order, which must name each of them once. Only while a draft (PD-012).</summary>
    public void ReorderWords(Level level, IReadOnlyList<Guid> wordIds, DateTime nowUtc)
    {
        EnsureStructureCanChange(level);
        if (wordIds.Count != _words.Count || wordIds.Distinct().Count() != wordIds.Count
            || !wordIds.All(id => _words.Any(word => word.Id == id)))
        {
            throw new DomainException("The order must name each word of the lesson once.");
        }

        Renumber(wordIds);
        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>
    /// Shows or hides a word in the learn step. Only while the lesson is a draft (owner, 2026-10-24: like the other
    /// structural changes of PD-012). Setting the current value changes nothing.
    /// </summary>
    public void SetLearnCard(Level level, Guid wordId, bool show, DateTime nowUtc)
    {
        EnsureStructureCanChange(level);
        var word = GetWord(wordId);
        if (word.ShowInLearnStep == show)
        {
            return;
        }

        word.ShowInLearnStepAs(show);
        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Adds an empty quiz after the other activities. Only while the lesson is a draft (PD-012).</summary>
    public QuizActivity AddQuiz(Level level, DateTime nowUtc)
    {
        EnsureStructureCanChange(level);
        EnsureRoomForActivity();

        var quiz = QuizActivity.Create(Id, _activities.Count + 1);
        _activities.Add(quiz);
        ContentChangedAtUtc = nowUtc;
        return quiz;
    }

    /// <summary>Adds a matching game after the other activities. Only while the lesson is a draft (PD-012).</summary>
    /// <param name="levelWords">For a boss: the words of the level's lessons.</param>
    public MatchingActivity AddMatching(
        Level level,
        MatchingMode mode,
        IReadOnlyList<Guid> wordIds,
        DateTime nowUtc,
        LevelWords? levelWords = null)
    {
        EnsureStructureCanChange(level);
        EnsureRoomForActivity();
        EnsureWordsAvailable(wordIds, ContentErrorCodes.InvalidMatching, levelWords);

        var matching = MatchingActivity.Create(Id, _activities.Count + 1, mode, wordIds);
        _activities.Add(matching);
        ContentChangedAtUtc = nowUtc;
        return matching;
    }

    /// <summary>
    /// Changes a matching's mode and words. Only while the lesson is a draft: a matching has no texts of its own to
    /// correct once published (PD-012).
    /// </summary>
    /// <param name="levelWords">For a boss: the words of the level's lessons.</param>
    public void UpdateMatching(
        Level level,
        Guid activityId,
        MatchingMode mode,
        IReadOnlyList<Guid> wordIds,
        DateTime nowUtc,
        LevelWords? levelWords = null)
    {
        EnsureStructureCanChange(level);
        var matching = GetActivity(activityId) as MatchingActivity
                       ?? throw new DomainException("The activity is not a matching.");
        EnsureWordsAvailable(wordIds, ContentErrorCodes.InvalidMatching, levelWords);
        matching.Change(mode, wordIds);
        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Removes an activity with its questions and closes the gap. Only while a draft (PD-012).</summary>
    public void RemoveActivity(Level level, Guid activityId, DateTime nowUtc)
    {
        EnsureStructureCanChange(level);
        _activities.Remove(GetActivity(activityId));
        var remaining = Activities;
        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].MoveTo(index + 1);
        }

        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Puts the activities in the given order, which must name each of them once. Only while a draft.</summary>
    public void ReorderActivities(Level level, IReadOnlyList<Guid> activityIds, DateTime nowUtc)
    {
        EnsureStructureCanChange(level);
        if (activityIds.Count != _activities.Count || activityIds.Distinct().Count() != activityIds.Count
            || !activityIds.All(id => _activities.Any(activity => activity.Id == id)))
        {
            throw new DomainException("The order must name each activity of the lesson once.");
        }

        for (var index = 0; index < activityIds.Count; index++)
        {
            GetActivity(activityIds[index]).MoveTo(index + 1);
        }

        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Adds a question at the end of a quiz. Only while the lesson is a draft (PD-012).</summary>
    /// <param name="levelWords">For a boss: the words of the level's lessons.</param>
    public QuizQuestion AddQuizQuestion(
        Level level,
        Guid activityId,
        QuizQuestionSpec spec,
        DateTime nowUtc,
        LevelWords? levelWords = null)
    {
        EnsureStructureCanChange(level);
        EnsureWordsAvailable(spec, levelWords);
        var question = GetQuiz(activityId).AddQuestion(spec);
        ContentChangedAtUtc = nowUtc;
        return question;
    }

    /// <summary>
    /// Changes a question. In a draft anything can change; once published only the sentence text can be corrected
    /// (PD-012), so the form, the answer and the options must stay the same.
    /// </summary>
    /// <param name="levelWords">For a boss: the words of the level's lessons.</param>
    public void UpdateQuizQuestion(
        Level level,
        Guid activityId,
        Guid questionId,
        QuizQuestionSpec spec,
        DateTime nowUtc,
        LevelWords? levelWords = null)
    {
        EnsureCanChange(level);
        EnsureNotArchived();
        var question = GetQuiz(activityId).GetQuestion(questionId);
        if (Status == ContentStatus.Draft)
        {
            EnsureWordsAvailable(spec, levelWords);
            question.Change(spec);
        }
        else if (question.Spec.HasSameStructureAs(spec))
        {
            question.CorrectSentence(spec.Sentence);
        }
        else
        {
            throw new DomainException(
                "A published question's sentence can be corrected, but not its answer or options. Unpublish the lesson first.",
                ContentErrorCodes.StructureLocked);
        }

        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Removes a question and closes the gap. Only while the lesson is a draft (PD-012).</summary>
    public void RemoveQuizQuestion(Level level, Guid activityId, Guid questionId, DateTime nowUtc)
    {
        EnsureStructureCanChange(level);
        GetQuiz(activityId).RemoveQuestion(questionId);
        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Puts a quiz's questions in the given order. Only while the lesson is a draft (PD-012).</summary>
    public void ReorderQuizQuestions(Level level, Guid activityId, IReadOnlyList<Guid> questionIds, DateTime nowUtc)
    {
        EnsureStructureCanChange(level);
        GetQuiz(activityId).ReorderQuestions(questionIds);
        ContentChangedAtUtc = nowUtc;
    }

    /// <summary>Whether any activity shows the word (for a boss: a word of one of the level's lessons).</summary>
    public bool UsesWord(Guid wordId) => _activities.Any(activity => activity.UsesWord(wordId));

    private LessonActivity GetActivity(Guid activityId) =>
        _activities.SingleOrDefault(activity => activity.Id == activityId)
        ?? throw new DomainException("The activity is not in this lesson.");

    private QuizActivity GetQuiz(Guid activityId) =>
        GetActivity(activityId) as QuizActivity ?? throw new DomainException("The activity is not a quiz.");

    private void EnsureRoomForActivity()
    {
        if (_activities.Count >= MaxActivities)
        {
            throw new DomainException($"A lesson has at most {MaxActivities} activities.", ContentErrorCodes.ActivityLimit);
        }
    }

    private void EnsureWordsAvailable(QuizQuestionSpec spec, LevelWords? levelWords) =>
        EnsureWordsAvailable([spec.CorrectWordId, .. spec.WrongWordIds], ContentErrorCodes.InvalidQuestion, levelWords);

    /// <summary>A lesson's activities use its own words; a boss's use the words of the level's lessons.</summary>
    private void EnsureWordsAvailable(IEnumerable<Guid> wordIds, string code, LevelWords? levelWords)
    {
        Func<Guid, bool> available;
        if (IsBoss)
        {
            var pool = levelWords ?? throw new InvalidOperationException("A boss needs the words of its level.");
            available = pool.Contains;
        }
        else
        {
            var own = _words.Select(word => word.Id).ToHashSet();
            available = own.Contains;
        }

        if (!wordIds.All(available))
        {
            throw new DomainException(
                IsBoss ? "Every word must be a word of this level's lessons." : "Every word must be a word of this lesson.",
                code);
        }
    }

    private void EnsureReadyToPublish(LevelWords? levelWords)
    {
        if (IsBoss)
        {
            EnsureBossReadyToPublish(levelWords ?? throw new InvalidOperationException("A boss needs the words of its level."));
        }
        else
        {
            EnsureWordBankReadyToPublish();
        }

        if (_activities.Count == 0)
        {
            throw new DomainException(
                "A lesson needs at least one activity before it is published.",
                ContentErrorCodes.LessonNotReady);
        }

        foreach (var activity in _activities)
        {
            activity.EnsureReadyToPublish();
        }
    }

    private void EnsureWordBankReadyToPublish()
    {
        if (_words.Count == 0)
        {
            throw new DomainException("A lesson needs at least one word before it is published.", ContentErrorCodes.LessonNotReady);
        }

        if (_words.Any(word => word.ImageUrl is null))
        {
            throw new DomainException("Every word needs a picture before the lesson is published.", ContentErrorCodes.LessonNotReady);
        }

        if (!_words.Any(word => word.ShowInLearnStep))
        {
            throw new DomainException(
                "The learn step needs at least one word card before the lesson is published.",
                ContentErrorCodes.LessonNotReady);
        }
    }

    private void EnsureBossReadyToPublish(LevelWords levelWords)
    {
        if (BossImageUrl is null)
        {
            throw new DomainException("The boss needs a picture before it is published.", ContentErrorCodes.LessonNotReady);
        }

        // The words live in the lessons, so a word may still lack its picture in a draft lesson.
        var missing = _activities.SelectMany(UsedWordIds).Distinct().Any(wordId => !levelWords.HasPicture(wordId));
        if (missing)
        {
            throw new DomainException(
                "Every word the boss uses needs a picture before the boss is published.",
                ContentErrorCodes.LessonNotReady);
        }
    }

    private static IEnumerable<Guid> UsedWordIds(LessonActivity activity) => activity switch
    {
        QuizActivity quiz => quiz.Questions.SelectMany(question => question.Options.Select(option => option.WordId)),
        MatchingActivity matching => matching.Pairs.Select(pair => pair.WordId),
        _ => [],
    };

    /// <summary>A published level must keep a published boss (owner, 2026-10-24).</summary>
    private void EnsureLevelKeepsBoss(Level level)
    {
        if (IsBoss && level.Status == ContentStatus.Published)
        {
            throw new DomainException(
                "A published level needs its boss. Unpublish the level first.",
                ContentErrorCodes.BossRequired);
        }
    }

    private void EnsureBoss()
    {
        if (!IsBoss)
        {
            throw new DomainException("Only a boss has a boss picture.");
        }
    }

    private void EnsureHasWordBank()
    {
        if (IsBoss)
        {
            throw new DomainException(
                "A boss uses the words of its level's lessons; it has no word bank of its own.",
                ContentErrorCodes.BossHasNoWordBank);
        }
    }

    private void EnsureCanChange(Level level)
    {
        if (level.Id != LevelId)
        {
            throw new DomainException("The lesson belongs to another level.");
        }

        level.EnsureLessonsCanChange();
    }

    /// <summary>Adding, removing and reordering are structural changes: a published lesson is locked (PD-012).</summary>
    private void EnsureStructureCanChange(Level level)
    {
        EnsureCanChange(level);
        EnsureNotArchived();
        if (Status != ContentStatus.Draft)
        {
            throw new DomainException(
                "A published lesson's words can be corrected but not added, removed or reordered. Unpublish it first.",
                ContentErrorCodes.StructureLocked);
        }
    }

    private LessonWord GetWord(Guid wordId) =>
        _words.SingleOrDefault(word => word.Id == wordId)
        ?? throw new DomainException("The word is not in this lesson.");

    private void EnsureNoOtherWord(string english, Guid? exceptWordId)
    {
        var normalized = LessonWord.Normalize(english);
        if (_words.Any(word => word.Id != exceptWordId && LessonWord.Normalize(word.English) == normalized))
        {
            throw new DomainException("The lesson already has this word.", ContentErrorCodes.DuplicateWord);
        }
    }

    private void Renumber(IReadOnlyList<Guid> wordIds)
    {
        for (var index = 0; index < wordIds.Count; index++)
        {
            _words.Single(word => word.Id == wordIds[index]).MoveTo(index + 1);
        }
    }
}
