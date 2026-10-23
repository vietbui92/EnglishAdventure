using MediatR;

namespace EnglishAdventure.Application.Family.Commands.RestoreChildProfile;

/// <summary>
/// Brings back an archived profile of the signed-in Parent, with its progress. Fails when the family already has the
/// maximum number of active profiles, or when an active profile now uses the same nickname.
/// </summary>
public sealed record RestoreChildProfileCommand(Guid ProfileId) : IRequest;
