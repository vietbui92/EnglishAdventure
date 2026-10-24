using EnglishAdventure.Application.Authentication.Models;
using MediatR;

namespace EnglishAdventure.Application.Family.Commands.StartChildSession;

/// <summary>
/// Switches this device to child mode for one of the signed-in Parent's active profiles (PD-010) and returns the child
/// session's tokens.
/// </summary>
/// <param name="CurrentRefreshToken">
/// The device's own session, which is ended so that leaving child mode needs the Parent to sign in again. Null when the
/// caller has none to give (for example an API tool); the Parent's session then stays.
/// </param>
public sealed record StartChildSessionCommand(Guid ProfileId, string? CurrentRefreshToken) : IRequest<TokenPair>;
