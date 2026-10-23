using MediatR;

namespace EnglishAdventure.Application.Authentication.Commands.ResendEmailConfirmation;

/// <param name="Language">"vi" or "en": the language of the email. Defaults to Vietnamese.</param>
public sealed record ResendEmailConfirmationCommand(string Email, string? Language) : IRequest;
