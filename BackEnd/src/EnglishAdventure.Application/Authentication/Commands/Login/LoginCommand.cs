using EnglishAdventure.Application.Authentication.Models;
using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<TokenPair>;
