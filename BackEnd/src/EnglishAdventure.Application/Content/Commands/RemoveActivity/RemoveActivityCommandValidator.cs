using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.RemoveActivity;

public sealed class RemoveActivityCommandValidator : AbstractValidator<RemoveActivityCommand>
{
    public RemoveActivityCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
