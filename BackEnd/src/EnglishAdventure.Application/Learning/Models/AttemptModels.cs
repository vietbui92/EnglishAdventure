using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Application.Learning.Models;

/// <summary>
/// A lesson attempt as its child sees it (S4.1): what is done, so play resumes where it stopped (PD-012), and the
/// result once finished. It uses the play view's identities (S3.4).
/// </summary>
/// <param name="Status">"InProgress" or "Finished".</param>
/// <param name="DoneItems">Questions answered and pairs matched, out of <paramref name="TotalItems"/>.</param>
/// <param name="Questions">The questions answered, with the right option (shown after answering).</param>
/// <param name="MatchedPairs">The pairs matched right; pairs only tried wrong are not listed.</param>
/// <param name="Result">Set once finished.</param>
public sealed record AttemptView(
    Guid AttemptId,
    Guid LessonId,
    string Status,
    int TotalItems,
    int DoneItems,
    IReadOnlyList<AnsweredQuestion> Questions,
    IReadOnlyList<MatchedPair> MatchedPairs,
    AttemptResult? Result)
{
    public static AttemptView From(LessonAttempt attempt, AnswerKey key)
    {
        var answers = attempt.Answers;
        var questions = answers
            .Where(answer => key.Question(answer.ItemId) is not null)
            .Select(answer => new AnsweredQuestion(
                answer.ActivityId,
                answer.ItemId,
                answer.ChosenId,
                answer.IsCorrect,
                key.Question(answer.ItemId)!.CorrectOptionId))
            .ToList();
        var pairs = answers
            .Where(answer => answer.IsCorrect && key.Pair(answer.ItemId) is not null)
            .Select(answer => new MatchedPair(answer.ActivityId, answer.ItemId, answer.ChosenId))
            .ToList();

        return new AttemptView(
            attempt.Id,
            attempt.LessonId,
            attempt.Status.ToString(),
            attempt.TotalItems,
            questions.Count + pairs.Count,
            questions,
            pairs,
            attempt.IsFinished
                ? new AttemptResult(attempt.CorrectItems!.Value, attempt.TotalItems, attempt.ScorePercent!.Value, attempt.Stars!.Value)
                : null);
    }
}

public sealed record AnsweredQuestion(Guid ActivityId, Guid QuestionId, Guid ChosenOptionId, bool Correct, Guid CorrectOptionId);

/// <param name="LeftId">The pair's left side (the English word).</param>
/// <param name="RightId">The right side it is matched with (its picture or meaning).</param>
public sealed record MatchedPair(Guid ActivityId, Guid LeftId, Guid RightId);

/// <param name="CorrectItems">Questions and pairs right on the first try.</param>
/// <param name="Stars">0 to 3: 60, 80 and 100% (PD-011). One star or more completes the lesson (S4.2).</param>
public sealed record AttemptResult(int CorrectItems, int TotalItems, int ScorePercent, int Stars);

/// <summary>One answer or match graded by the server, with the attempt after it.</summary>
/// <param name="CorrectOptionId">For a quiz question: the right option. Null for a match, so a wrong try gives nothing away.</param>
public sealed record AnswerResult(bool Correct, Guid? CorrectOptionId, AttemptView Attempt);
