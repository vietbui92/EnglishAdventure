using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One answer choice of a quiz question: a word of the lesson. Whether it is correct is not stored here but follows
/// from the question's <see cref="QuizQuestion.CorrectWordId"/>, so a question can never have two correct options.
/// </summary>
public sealed class QuizOption : Entity
{
    private QuizOption()
    {
    }

    public Guid QuestionId { get; private set; }

    public Guid WordId { get; private set; }

    public int SortOrder { get; private set; }

    internal static QuizOption Create(Guid questionId, Guid wordId, int sortOrder) =>
        new() { Id = Guid.NewGuid(), QuestionId = questionId, WordId = wordId, SortOrder = sortOrder };

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}
