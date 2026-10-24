using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.CreateLesson;

public sealed class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(command => command.Title).ValidTitle();
    }
}
