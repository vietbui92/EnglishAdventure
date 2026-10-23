using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Authentication.Models;

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    AccountType AccountType,
    bool IsActive,
    bool IsLockedOut);
