using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : IRequest;
