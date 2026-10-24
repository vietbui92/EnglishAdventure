using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.RemoveLessonWord;

public sealed class RemoveLessonWordCommandValidator : AbstractValidator<RemoveLessonWordCommand>
{
    public RemoveLessonWordCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
