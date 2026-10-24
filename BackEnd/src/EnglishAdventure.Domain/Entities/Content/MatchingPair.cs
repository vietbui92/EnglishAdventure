using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One pair of a matching activity: a word of the lesson. Its English text is one side, its picture or meaning the
/// other, so a pair always has exactly one right partner and the two sides never need typing again.
/// </summary>
public sealed class MatchingPair : Entity
{
    private MatchingPair()
    {
    }

    public Guid ActivityId { get; private set; }

    public Guid WordId { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>
    /// The identity children see for the pair's right side (picture or meaning), different from <see cref="Entity.Id"/>
    /// (the left side), so the play view never shows which right side belongs to which word (S3.4).
    /// </summary>
    public Guid ChoiceId { get; private set; }

    internal static MatchingPair Create(Guid activityId, Guid wordId, int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            WordId = wordId,
            SortOrder = sortOrder,
            ChoiceId = Guid.NewGuid(),
        };

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}
