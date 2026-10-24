using EnglishAdventure.Application.Family.Models;
using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Authentication.Models;

/// <param name="Session"><see cref="UserSession"/> or <see cref="ChildSession"/>.</param>
/// <param name="ChildProfile">The profile a child session learns as; null for a user's own session.</param>
public sealed record CurrentUserProfile(
    Guid UserId,
    string Email,
    UserType UserType,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string Session,
    ChildProfileSummary? ChildProfile)
{
    public const string UserSession = "user";
    public const string ChildSession = "child";
}
