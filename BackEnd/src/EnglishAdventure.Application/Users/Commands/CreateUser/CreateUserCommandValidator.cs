using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Domain.Entities.Identity;
using FluentValidation;

namespace EnglishAdventure.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public static readonly string[] CreatableUserTypes = ["Admin", "Parent"];

    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(User.EmailMaxLength);

        RuleFor(command => command.Password)
            .StrongPassword();

        RuleFor(command => command.UserType)
            .Must(type => CreatableUserTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("The user type must be 'Admin' or 'Parent'. Child users are not available yet.");
    }
}
