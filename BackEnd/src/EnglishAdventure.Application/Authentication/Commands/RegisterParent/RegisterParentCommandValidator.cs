using EnglishAdventure.Domain.Entities.Identity;
using FluentValidation;

namespace EnglishAdventure.Application.Authentication.Commands.RegisterParent;

public sealed class RegisterParentCommandValidator : AbstractValidator<RegisterParentCommand>
{
    public RegisterParentCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(User.EmailMaxLength);

        RuleFor(command => command.Password)
            .StrongPassword();

        RuleFor(command => command.AcceptTerms)
            .Equal(true)
            .WithMessage("The terms and privacy policy must be accepted.");

        RuleFor(command => command.Language)
            .Must(language => language is null || AuthEmails.Languages.Contains(language))
            .WithMessage("The language must be 'vi' or 'en'.");
    }
}
