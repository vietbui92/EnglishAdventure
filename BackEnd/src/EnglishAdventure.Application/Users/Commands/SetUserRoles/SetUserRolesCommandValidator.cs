using EnglishAdventure.Domain.Entities.Identity;
using FluentValidation;

namespace EnglishAdventure.Application.Users.Commands.SetUserRoles;

public sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(command => command.RoleNames)
            .NotNull()
            .Must(names => names.Count <= 20).WithMessage("A user cannot have more than 20 roles.")
            .Must(names => names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count)
            .WithMessage("Each role can be listed only once.");

        RuleForEach(command => command.RoleNames)
            .NotEmpty()
            .MaximumLength(Role.NameMaxLength);
    }
}
