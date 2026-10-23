using EnglishAdventure.Domain.Entities.Identity;
using FluentValidation;

namespace EnglishAdventure.Application.Authentication.Commands.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationCommandValidator : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(User.EmailMaxLength);

        RuleFor(command => command.Language)
            .Must(language => language is null || AuthEmails.Languages.Contains(language))
            .WithMessage("The language must be 'vi' or 'en'.");
    }
}
