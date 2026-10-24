using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UnpublishLevel;

public sealed class UnpublishLevelCommandValidator : AbstractValidator<UnpublishLevelCommand>
{
    public UnpublishLevelCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
