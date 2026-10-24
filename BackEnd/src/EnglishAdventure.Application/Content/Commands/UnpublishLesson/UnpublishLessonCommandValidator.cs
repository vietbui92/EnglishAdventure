using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UnpublishLesson;

public sealed class UnpublishLessonCommandValidator : AbstractValidator<UnpublishLessonCommand>
{
    public UnpublishLessonCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
