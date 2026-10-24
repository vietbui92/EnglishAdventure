using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.ReorderLessonWords;

public sealed class ReorderLessonWordsCommandValidator : AbstractValidator<ReorderLessonWordsCommand>
{
    public ReorderLessonWordsCommandValidator()
    {
        RuleFor(command => command.WordIds).ValidOrder();
        RuleFor(command => command.Version).ValidVersion();
    }
}
