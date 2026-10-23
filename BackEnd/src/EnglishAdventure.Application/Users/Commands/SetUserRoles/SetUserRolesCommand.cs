using MediatR;

namespace EnglishAdventure.Application.Users.Commands.SetUserRoles;

/// <summary>
/// Replaces the user's roles with exactly <paramref name="RoleNames"/> (an empty list removes all roles).
/// </summary>
public sealed record SetUserRolesCommand(Guid UserId, IReadOnlyList<string> RoleNames) : IRequest;
