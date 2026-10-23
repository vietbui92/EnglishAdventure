using EnglishAdventure.Application.Authentication.Models;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<TokenPair>;
