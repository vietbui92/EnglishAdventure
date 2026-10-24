using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// What levels and lessons share (S3.1, PD-012): a title, a place in an order, and the Draft → Published → Archived
/// life. Archived is final: retired content is replaced by new content, never brought back or changed.
/// </summary>
public abstract class OrderedContent : Entity
{
    public ContentTitle Title { get; private set; } = null!;

    /// <summary>The position among the non-archived items of the same list, starting at 1.</summary>
    public int SortOrder { get; private set; }

    public ContentStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public DateTime? ArchivedAtUtc { get; private set; }

    public bool IsArchived => Status == ContentStatus.Archived;

    protected void Initialize(ContentTitle title, int sortOrder, DateTime nowUtc)
    {
        Id = Guid.NewGuid();
        Title = title;
        SortOrder = ValidSortOrder(sortOrder);
        Status = ContentStatus.Draft;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Changing the title is allowed while published: PD-012 lets Admins correct texts.</summary>
    protected void ChangeTitle(ContentTitle title)
    {
        EnsureNotArchived();
        Title = title;
    }

    protected void MoveToCore(int sortOrder)
    {
        EnsureNotArchived();
        SortOrder = ValidSortOrder(sortOrder);
    }

    /// <summary>Publishing published content changes nothing.</summary>
    protected void PublishCore(DateTime nowUtc)
    {
        EnsureNotArchived();
        if (Status == ContentStatus.Published)
        {
            return;
        }

        Status = ContentStatus.Published;
        PublishedAtUtc = nowUtc;
    }

    /// <summary>Unpublishing a draft changes nothing.</summary>
    /// <param name="learnersStarted">Whether a child has already started this content (PD-012).</param>
    protected void UnpublishCore(bool learnersStarted)
    {
        EnsureNotArchived();
        if (Status == ContentStatus.Draft)
        {
            return;
        }

        if (learnersStarted)
        {
            throw new DomainException(
                "Children have already started this content, so it cannot be unpublished. Archive it and create a new one instead.");
        }

        Status = ContentStatus.Draft;
        PublishedAtUtc = null;
    }

    /// <summary>Archiving archived content changes nothing.</summary>
    public void Archive(DateTime nowUtc)
    {
        if (IsArchived)
        {
            return;
        }

        Status = ContentStatus.Archived;
        ArchivedAtUtc = nowUtc;
    }

    protected void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new DomainException("Archived content cannot be changed. Create new content instead.");
        }
    }

    private static int ValidSortOrder(int sortOrder) =>
        sortOrder >= 1 ? sortOrder : throw new DomainException("A position starts at 1.");
}
