using EnglishAdventure.Domain.Entities.Content;

namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>
/// The answers of a lesson, built from its content on the server (S4.1). It speaks the identities of the play view
/// (S3.4): quiz options by their own Id, a matching's left side by the pair's Id and its right side by the pair's
/// <see cref="MatchingPair.ChoiceId"/>. Nothing of it is ever sent before a child has answered.
/// </summary>
public sealed class AnswerKey
{
    private readonly Dictionary<Guid, QuestionKey> _questions;
    private readonly Dictionary<Guid, PairKey> _pairs;

    private AnswerKey(Dictionary<Guid, QuestionKey> questions, Dictionary<Guid, PairKey> pairs)
    {
        _questions = questions;
        _pairs = pairs;
    }

    /// <summary>Every quiz question and every matching pair scores one item.</summary>
    public int TotalItems => _questions.Count + _pairs.Count;

    public static AnswerKey For(Lesson lesson)
    {
        var questions = new Dictionary<Guid, QuestionKey>();
        var pairs = new Dictionary<Guid, PairKey>();
        foreach (var activity in lesson.Activities)
        {
            switch (activity)
            {
                case QuizActivity quiz:
                    foreach (var question in quiz.Questions)
                    {
                        var correct = question.Options.Single(question.IsCorrect);
                        questions[question.Id] = new QuestionKey(
                            quiz.Id,
                            correct.Id,
                            question.Options.Select(option => option.Id).ToHashSet());
                    }

                    break;
                case MatchingActivity matching:
                    foreach (var pair in matching.Pairs)
                    {
                        pairs[pair.Id] = new PairKey(
                            matching.Id,
                            pair.ChoiceId,
                            matching.Pairs.Select(other => other.ChoiceId).ToHashSet());
                    }

                    break;
            }
        }

        return new AnswerKey(questions, pairs);
    }

    public QuestionKey? Question(Guid questionId) => _questions.GetValueOrDefault(questionId);

    public PairKey? Pair(Guid leftId) => _pairs.GetValueOrDefault(leftId);

    /// <param name="ActivityId">The quiz the question belongs to.</param>
    /// <param name="OptionIds">The options a child may choose from.</param>
    public sealed record QuestionKey(Guid ActivityId, Guid CorrectOptionId, IReadOnlySet<Guid> OptionIds);

    /// <param name="ActivityId">The matching the pair belongs to.</param>
    /// <param name="RightIds">The right sides of the same matching, the only ones a left side may be matched with.</param>
    public sealed record PairKey(Guid ActivityId, Guid RightId, IReadOnlySet<Guid> RightIds);
}
