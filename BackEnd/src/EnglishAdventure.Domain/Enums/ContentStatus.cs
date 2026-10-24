namespace EnglishAdventure.Domain.Enums;

/// <summary>Where a level or lesson is in its life (S3.1, PD-012).</summary>
public enum ContentStatus
{
    /// <summary>Being prepared; only Admins see it.</summary>
    Draft = 1,

    /// <summary>Visible to learners. Its structure is locked; texts can still be corrected.</summary>
    Published = 2,

    /// <summary>Retired for good: hidden from learners, kept for their past results, never changed again.</summary>
    Archived = 3
}
