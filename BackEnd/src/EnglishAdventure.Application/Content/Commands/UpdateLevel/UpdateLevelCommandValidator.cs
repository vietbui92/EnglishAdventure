using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UpdateLevel;

public sealed class UpdateLevelCommandValidator : AbstractValidator<UpdateLevelCommand>
{
    public UpdateLevelCommandValidator()
    {
        RuleFor(command => command.Title).ValidTitle();
        RuleFor(command => command.Description).ValidDescription();
        RuleFor(command => command.Version).ValidVersion();
    }
}
