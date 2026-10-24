using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.SetLearnCard;

public sealed class SetLearnCardCommandValidator : AbstractValidator<SetLearnCardCommand>
{
    public SetLearnCardCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
