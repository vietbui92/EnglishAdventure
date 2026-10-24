using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.Services;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Learning;

/// <summary>
/// One child playing one lesson or boss, from the first answer to the stars (S4.1). The server grades every answer
/// against the lesson's <see cref="AnswerKey"/>; a client never sends a score. Answers are saved one by one, so the
/// child can leave and come back (PD-012). Owner decisions 2026-10-24: a quiz question is answered once and its first
/// answer counts; a matching pair is tried until right and counts when right on the first try.
/// </summary>
public sealed class LessonAttempt : Entity
{
    private readonly List<AttemptAnswer> _answers = new();

    private LessonAttempt()
    {
    }

    public Guid ChildProfileId { get; private set; }

    public Guid LessonId { get; private set; }

    public AttemptStatus Status { get; private set; }

    /// <summary>The questions and pairs of the lesson when the attempt started.</summary>
    public int TotalItems { get; private set; }

    /// <summary>Items right on the first try; set when finished.</summary>
    public int? CorrectItems { get; private set; }

    public int? ScorePercent { get; private set; }

    /// <summary>0 to 3 (PD-011); set when finished.</summary>
    public int? Stars { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    /// <summary>
    /// Saved with every answer, so two answers at the same moment cannot both pass the attempt's concurrency check: the
    /// second gets a conflict and is sent again, and the attempt always sees its last item and finishes.
    /// </summary>
    public DateTime? LastAnsweredAtUtc { get; private set; }

    public bool IsFinished => Status == AttemptStatus.Finished;

    public IReadOnlyList<AttemptAnswer> Answers =>
        _answers.OrderBy(answer => answer.AnsweredAtUtc).ThenBy(answer => answer.TryNumber).ToList();

    public static LessonAttempt Start(Guid childProfileId, Guid lessonId, AnswerKey key, DateTime nowUtc)
    {
        if (childProfileId == Guid.Empty || lessonId == Guid.Empty)
        {
            throw new DomainException("An attempt needs its child and its lesson.");
        }

        if (key.TotalItems == 0)
        {
            throw new DomainException("The lesson has nothing to play.");
        }

        return new LessonAttempt
        {
            Id = Guid.NewGuid(),
            ChildProfileId = childProfileId,
            LessonId = lessonId,
            Status = AttemptStatus.InProgress,
            TotalItems = key.TotalItems,
            StartedAtUtc = nowUtc,
        };
    }

    public bool IsFor(Guid childProfileId) => ChildProfileId == childProfileId;

    /// <summary>
    /// Grades a quiz answer. A question already answered keeps its first answer: the same result comes back whatever
    /// is sent again, so a double tap or a retry is safe.
    /// </summary>
    public AnswerOutcome AnswerQuestion(AnswerKey key, Guid questionId, Guid optionId, DateTime nowUtc)
    {
        var question = key.Question(questionId) ?? throw Invalid("The question is not in this lesson.");
        var answered = _answers.SingleOrDefault(answer => answer.ItemId == questionId);
        if (answered is not null)
        {
            return new AnswerOutcome(answered.IsCorrect, question.CorrectOptionId, Repeated: true);
        }

        EnsureInProgress();
        if (!question.OptionIds.Contains(optionId))
        {
            throw Invalid("The option is not one of the question's.");
        }

        var correct = optionId == question.CorrectOptionId;
        _answers.Add(AttemptAnswer.Create(Id, question.ActivityId, questionId, optionId, correct, tryNumber: 1, nowUtc));
        LastAnsweredAtUtc = nowUtc;
        FinishWhenDone(key, nowUtc);
        return new AnswerOutcome(correct, question.CorrectOptionId, Repeated: false);
    }

    /// <summary>
    /// Checks one match of a matching. A wrong match is saved and the pair can be tried again; a pair already matched
    /// stays matched, and trying it again changes nothing. The right partner is never revealed by a wrong try.
    /// </summary>
    public AnswerOutcome MatchPair(AnswerKey key, Guid leftId, Guid rightId, DateTime nowUtc)
    {
        var pair = key.Pair(leftId) ?? throw Invalid("The pair is not in this lesson.");
        var tries = _answers.Where(answer => answer.ItemId == leftId).ToList();
        if (tries.Any(answer => answer.IsCorrect))
        {
            return new AnswerOutcome(true, CorrectChoiceId: null, Repeated: true);
        }

        EnsureInProgress();
        if (!pair.RightIds.Contains(rightId))
        {
            throw Invalid("The right side is not one of the matching's.");
        }

        var correct = rightId == pair.RightId;
        _answers.Add(AttemptAnswer.Create(Id, pair.ActivityId, leftId, rightId, correct, tries.Count + 1, nowUtc));
        LastAnsweredAtUtc = nowUtc;
        FinishWhenDone(key, nowUtc);
        return new AnswerOutcome(correct, CorrectChoiceId: null, Repeated: false);
    }

    /// <summary>Finished once every question is answered and every pair is matched; then scored.</summary>
    private void FinishWhenDone(AnswerKey key, DateTime nowUtc)
    {
        var items = _answers.GroupBy(answer => answer.ItemId).ToList();
        // A question is done once answered; a pair once matched right.
        var done = items.Count(group => key.Question(group.Key) is not null || group.Any(answer => answer.IsCorrect));
        if (done < TotalItems)
        {
            return;
        }

        CorrectItems = items.Count(group => group.Single(answer => answer.TryNumber == 1).IsCorrect);
        ScorePercent = LessonScoring.Percent(CorrectItems.Value, TotalItems);
        Stars = LessonScoring.Stars(ScorePercent.Value);
        Status = AttemptStatus.Finished;
        FinishedAtUtc = nowUtc;
    }

    private void EnsureInProgress()
    {
        if (IsFinished)
        {
            throw new DomainException("The attempt is finished.", LearningErrorCodes.AttemptFinished);
        }
    }

    private static DomainException Invalid(string message) => new(message, LearningErrorCodes.InvalidAnswer);
}
