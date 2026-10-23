using MediatR;

namespace EnglishAdventure.Application.Family.Commands.UpdateChildProfile;

/// <summary>Changes the nickname and avatar of one of the signed-in Parent's active profiles.</summary>
public sealed record UpdateChildProfileCommand(Guid ProfileId, string Nickname, string AvatarKey) : IRequest;
