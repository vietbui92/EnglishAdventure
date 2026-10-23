using MediatR;

namespace EnglishAdventure.Application.Family.Commands.ArchiveChildProfile;

/// <summary>Hides one of the signed-in Parent's profiles; its progress is kept. Archiving twice changes nothing.</summary>
public sealed record ArchiveChildProfileCommand(Guid ProfileId) : IRequest;
