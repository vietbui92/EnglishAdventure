using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UpdateLesson;

public sealed class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(command => command.Title).ValidTitle();
        RuleFor(command => command.Version).ValidVersion();
    }
}
