using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.ReorderLevels;

public sealed class ReorderLevelsCommandValidator : AbstractValidator<ReorderLevelsCommand>
{
    public ReorderLevelsCommandValidator()
    {
        RuleFor(command => command.LevelIds).ValidOrder();
    }
}
