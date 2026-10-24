using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// A stage of the adventure that groups lessons, for example "Animal Island" (S3.1, PD-011). Lessons point to it by
/// <see cref="Lesson.LevelId"/>; they are their own aggregates, so the level holds no lesson collection.
/// </summary>
public sealed class Level : OrderedContent
{
    public const int DescriptionMaxLength = 500;

    private Level()
    {
    }

    public string? Description { get; private set; }

    public static Level Create(ContentTitle title, string? description, int sortOrder, DateTime nowUtc)
    {
        var level = new Level { Description = ValidDescription(description) };
        level.Initialize(title, sortOrder, nowUtc);
        return level;
    }

    public void Update(ContentTitle title, string? description)
    {
        ChangeTitle(title);
        Description = ValidDescription(description);
    }

    public void MoveTo(int sortOrder) => MoveToCore(sortOrder);

    /// <param name="hasPublishedBoss">Whether the level's boss is published: a level needs it (S3.8, owner 2026-10-24).</param>
    public void Publish(DateTime nowUtc, bool hasPublishedBoss) =>
        PublishCore(nowUtc, () =>
        {
            if (!hasPublishedBoss)
            {
                throw new DomainException(
                    "A level is published only with a published boss challenge.",
                    ContentErrorCodes.LevelNeedsBoss);
            }
        });

    /// <param name="learnersStarted">Whether a child has started any lesson of this level.</param>
    public void Unpublish(bool learnersStarted) => UnpublishCore(learnersStarted);

    /// <summary>The lessons of an archived level are frozen with it.</summary>
    public void EnsureLessonsCanChange()
    {
        if (IsArchived)
        {
            throw new DomainException("The lessons of an archived level cannot be changed.", ContentErrorCodes.Archived);
        }
    }

    /// <summary>Trimmed; blank means no description.</summary>
    private static string? ValidDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length <= DescriptionMaxLength
            ? trimmed
            : throw new DomainException($"A description has at most {DescriptionMaxLength} characters.");
    }
}
