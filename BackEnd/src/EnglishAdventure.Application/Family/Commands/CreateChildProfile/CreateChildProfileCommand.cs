using MediatR;

namespace EnglishAdventure.Application.Family.Commands.CreateChildProfile;

/// <summary>Adds a child profile to the signed-in Parent's family. Returns its Id.</summary>
public sealed record CreateChildProfileCommand(string Nickname, string AvatarKey) : IRequest<Guid>;
