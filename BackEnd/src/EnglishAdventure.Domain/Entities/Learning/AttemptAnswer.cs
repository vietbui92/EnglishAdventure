using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.Entities.Learning;

/// <summary>
/// One try in a lesson attempt (S4.1): a quiz question answered (once), or a matching pair tried (until right). Only
/// the first try of an item counts for the score. Owned by <see cref="LessonAttempt"/>.
/// </summary>
public sealed class AttemptAnswer : Entity
{
    private AttemptAnswer()
    {
    }

    public Guid AttemptId { get; private set; }

    public Guid ActivityId { get; private set; }

    /// <summary>The question's Id, or the pair's Id (its left side).</summary>
    public Guid ItemId { get; private set; }

    /// <summary>The option chosen, or the right side the left side was matched with.</summary>
    public Guid ChosenId { get; private set; }

    public bool IsCorrect { get; private set; }

    /// <summary>1 for the first try of the item; matching pairs can be tried again after a wrong one.</summary>
    public int TryNumber { get; private set; }

    public DateTime AnsweredAtUtc { get; private set; }

    internal static AttemptAnswer Create(
        Guid attemptId,
        Guid activityId,
        Guid itemId,
        Guid chosenId,
        bool isCorrect,
        int tryNumber,
        DateTime nowUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            AttemptId = attemptId,
            ActivityId = activityId,
            ItemId = itemId,
            ChosenId = chosenId,
            IsCorrect = isCorrect,
            TryNumber = tryNumber,
            AnsweredAtUtc = nowUtc,
        };
}
