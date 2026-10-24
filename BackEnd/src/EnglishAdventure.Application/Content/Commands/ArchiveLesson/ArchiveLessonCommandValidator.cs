using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.ArchiveLesson;

public sealed class ArchiveLessonCommandValidator : AbstractValidator<ArchiveLessonCommand>
{
    public ArchiveLessonCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
