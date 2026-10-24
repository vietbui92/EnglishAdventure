using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.CreateLevel;

public sealed class CreateLevelCommandValidator : AbstractValidator<CreateLevelCommand>
{
    public CreateLevelCommandValidator()
    {
        RuleFor(command => command.Title).ValidTitle();
        RuleFor(command => command.Description).ValidDescription();
    }
}
