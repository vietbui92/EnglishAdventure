using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Authentication.Models;

public sealed record CurrentUserProfile(
    Guid UserId,
    string Email,
    UserType UserType,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
