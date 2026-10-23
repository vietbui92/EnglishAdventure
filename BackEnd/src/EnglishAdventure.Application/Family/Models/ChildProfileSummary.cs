namespace EnglishAdventure.Application.Family.Models;

/// <param name="Status">"Active" or "Archived".</param>
public sealed record ChildProfileSummary(
    Guid Id,
    string Nickname,
    string AvatarKey,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ArchivedAtUtc);
