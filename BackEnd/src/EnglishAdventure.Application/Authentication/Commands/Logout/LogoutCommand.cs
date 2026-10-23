using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;
