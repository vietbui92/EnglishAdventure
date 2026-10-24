using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One playable unit of a level and the unit that is completed and unlocked (PD-002, PD-011). Its words, activities
/// and boss challenge are added in S3.6–S3.8. Every change needs its level, because an archived level freezes its
/// lessons.
/// </summary>
public sealed class Lesson : OrderedContent
{
    private Lesson()
    {
    }

    public Guid LevelId { get; private set; }

    public static Lesson Create(Level level, ContentTitle title, int sortOrder, DateTime nowUtc)
    {
        level.EnsureLessonsCanChange();

        var lesson = new Lesson { LevelId = level.Id };
        lesson.Initialize(title, sortOrder, nowUtc);
        return lesson;
    }

    public void Rename(Level level, ContentTitle title)
    {
        EnsureCanChange(level);
        ChangeTitle(title);
    }

    public void MoveTo(Level level, int sortOrder)
    {
        EnsureCanChange(level);
        MoveToCore(sortOrder);
    }

    public void Publish(Level level, DateTime nowUtc)
    {
        EnsureCanChange(level);
        PublishCore(nowUtc);
    }

    /// <param name="learnersStarted">Whether a child has started this lesson.</param>
    public void Unpublish(Level level, bool learnersStarted)
    {
        EnsureCanChange(level);
        UnpublishCore(learnersStarted);
    }

    private void EnsureCanChange(Level level)
    {
        if (level.Id != LevelId)
        {
            throw new DomainException("The lesson belongs to another level.");
        }

        level.EnsureLessonsCanChange();
    }
}
