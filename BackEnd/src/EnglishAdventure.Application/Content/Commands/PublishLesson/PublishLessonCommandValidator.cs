using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.PublishLesson;

public sealed class PublishLessonCommandValidator : AbstractValidator<PublishLessonCommand>
{
    public PublishLessonCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
