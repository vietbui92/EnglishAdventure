namespace EnglishAdventure.Application.Users.Models;

public sealed record UserSummary(
    Guid Id,
    string Email,
    string UserType,
    bool IsActive,
    bool IsLockedOut,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);
