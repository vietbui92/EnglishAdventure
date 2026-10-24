namespace EnglishAdventure.Application.Content.Models;

/// <param name="Status">"Draft", "Published" or "Archived".</param>
/// <param name="Version">Send it back with a change; a stale version answers 409.</param>
public sealed record LevelSummary(
    Guid Id,
    string Title,
    string? Description,
    int SortOrder,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? ArchivedAtUtc,
    LessonCounts Lessons,
    string Version);

public sealed record LessonCounts(int Draft, int Published, int Archived);

public sealed record LevelDetails(LevelSummary Level, IReadOnlyList<LessonSummary> Lessons);

/// <param name="Status">"Draft", "Published" or "Archived".</param>
/// <param name="Version">Send it back with a change; a stale version answers 409.</param>
public sealed record LessonSummary(
    Guid Id,
    Guid LevelId,
    string Title,
    int SortOrder,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? ArchivedAtUtc,
    string Version);
