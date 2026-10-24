using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.ReorderLessons;

public sealed class ReorderLessonsCommandValidator : AbstractValidator<ReorderLessonsCommand>
{
    public ReorderLessonsCommandValidator()
    {
        RuleFor(command => command.LessonIds).ValidOrder();
    }
}
