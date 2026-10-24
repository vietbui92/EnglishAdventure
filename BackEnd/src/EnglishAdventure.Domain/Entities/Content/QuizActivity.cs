using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>A quiz (S3.2): ordered questions, each answered by picking one option.</summary>
public sealed class QuizActivity : LessonActivity
{
    /// <summary>Owner decision 2026-10-24.</summary>
    public const int MaxQuestions = 10;

    private readonly List<QuizQuestion> _questions = new();

    private QuizActivity()
    {
    }

    public override ActivityType Type => ActivityType.Quiz;

    public IReadOnlyList<QuizQuestion> Questions => _questions.OrderBy(question => question.SortOrder).ToList();

    internal static QuizActivity Create(Guid lessonId, int sortOrder)
    {
        var quiz = new QuizActivity { Id = Guid.NewGuid(), LessonId = lessonId };
        quiz.MoveTo(sortOrder);
        return quiz;
    }

    internal QuizQuestion AddQuestion(QuizQuestionSpec spec)
    {
        if (_questions.Count >= MaxQuestions)
        {
            throw new DomainException($"A quiz has at most {MaxQuestions} questions.", ContentErrorCodes.QuestionLimit);
        }

        var question = QuizQuestion.Create(Id, _questions.Count + 1, spec);
        _questions.Add(question);
        return question;
    }

    internal QuizQuestion GetQuestion(Guid questionId) =>
        _questions.SingleOrDefault(question => question.Id == questionId)
        ?? throw new DomainException("The question is not in this quiz.");

    internal void RemoveQuestion(Guid questionId)
    {
        _questions.Remove(GetQuestion(questionId));
        var remaining = Questions;
        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].MoveTo(index + 1);
        }
    }

    internal void ReorderQuestions(IReadOnlyList<Guid> questionIds)
    {
        if (questionIds.Count != _questions.Count || questionIds.Distinct().Count() != questionIds.Count
            || !questionIds.All(id => _questions.Any(question => question.Id == id)))
        {
            throw new DomainException("The order must name each question of the quiz once.");
        }

        for (var index = 0; index < questionIds.Count; index++)
        {
            GetQuestion(questionIds[index]).MoveTo(index + 1);
        }
    }

    internal override void EnsureReadyToPublish()
    {
        if (_questions.Count == 0)
        {
            throw new DomainException("Every quiz needs at least one question.", ContentErrorCodes.LessonNotReady);
        }
    }

    internal override bool UsesWord(Guid wordId) => _questions.Any(question => question.UsesWord(wordId));
}
