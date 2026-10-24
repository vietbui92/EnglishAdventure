using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.PublishLevel;

public sealed class PublishLevelCommandValidator : AbstractValidator<PublishLevelCommand>
{
    public PublishLevelCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
