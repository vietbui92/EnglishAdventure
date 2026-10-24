using EnglishAdventure.Application.Content;
using EnglishAdventure.Domain.Entities.Rewards;
using FluentValidation;

namespace EnglishAdventure.Application.Rewards.Commands.UpdateBadgeDefinition;

public sealed class UpdateBadgeDefinitionCommandValidator : AbstractValidator<UpdateBadgeDefinitionCommand>
{
    public UpdateBadgeDefinitionCommandValidator()
    {
        RuleFor(command => command.Kind)
            .Must(kind => BadgeKinds.TryParse(kind, out _))
            .WithMessage("The badge must be FirstLesson, Perfect, LevelConqueror or LevelSuperstar.");
        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= BadgeDefinition.NameMaxLength)
            .WithMessage($"A name of 1 to {BadgeDefinition.NameMaxLength} characters is required.");
        RuleFor(command => command.Description)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= BadgeDefinition.DescriptionMaxLength)
            .WithMessage($"A description of 1 to {BadgeDefinition.DescriptionMaxLength} characters is required.");
        RuleFor(command => command.ImageUrl).ValidOptionalImageUrl();
    }
}
