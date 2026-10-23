using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Authentication.Models;

public sealed record CurrentUserProfile(
    Guid UserId,
    string Email,
    AccountType AccountType,
    IReadOnlyList<string> Roles);
