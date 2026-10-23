using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Domain.Entities.Identity;
using FluentValidation;

namespace EnglishAdventure.Application.Authorization.Commands.BootstrapSuperAdmin;

public sealed class BootstrapSuperAdminCommandValidator : AbstractValidator<BootstrapSuperAdminCommand>
{
    public BootstrapSuperAdminCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(User.EmailMaxLength);

        RuleFor(command => command.Password)
            .StrongPassword();
    }
}
