using MediatR;

namespace EnglishAdventure.Application.Users.Commands.SetUserActive;

/// <summary>
/// Activates or deactivates a user. Deactivation ends all of the user's sessions; its permissions stop
/// working within the permission cache time (1 minute).
/// </summary>
public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest;
