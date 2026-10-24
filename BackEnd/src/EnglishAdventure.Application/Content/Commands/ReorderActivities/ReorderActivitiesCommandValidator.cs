using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.ReorderActivities;

public sealed class ReorderActivitiesCommandValidator : AbstractValidator<ReorderActivitiesCommand>
{
    public ReorderActivitiesCommandValidator()
    {
        RuleFor(command => command.ActivityIds).ValidOrder();
        RuleFor(command => command.Version).ValidVersion();
    }
}
