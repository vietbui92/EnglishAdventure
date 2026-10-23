using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.RegisterParent;

/// <param name="Language">"vi" or "en": the language of the emails. Defaults to Vietnamese.</param>
public sealed record RegisterParentCommand(string Email, string Password, bool AcceptTerms, string? Language) : IRequest;
