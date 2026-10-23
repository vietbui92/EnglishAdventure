using FluentValidation;

namespace EnglishAdventure.Application.Authentication.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(256);
    }
}
