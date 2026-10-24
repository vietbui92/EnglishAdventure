using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One game of a lesson (PD-003): a quiz now, matching in S3.3. The kinds share this small base instead of a generic
/// game engine. Owned by <see cref="Lesson"/>, which changes it.
/// </summary>
public abstract class LessonActivity : Entity
{
    protected LessonActivity()
    {
    }

    public Guid LessonId { get; protected set; }

    /// <summary>The position among the lesson's activities, starting at 1.</summary>
    public int SortOrder { get; private set; }

    public abstract ActivityType Type { get; }

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;

    /// <summary>Throws when the activity cannot be played yet.</summary>
    internal abstract void EnsureReadyToPublish();

    /// <summary>Whether the activity shows the word, so the word cannot leave the word bank.</summary>
    internal abstract bool UsesWord(Guid wordId);
}
