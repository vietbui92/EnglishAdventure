using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Authentication.Models;

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    UserType UserType,
    bool IsActive,
    bool IsLockedOut);
