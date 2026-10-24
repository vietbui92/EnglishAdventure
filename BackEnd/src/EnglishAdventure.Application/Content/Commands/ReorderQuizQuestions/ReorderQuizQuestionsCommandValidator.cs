using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.ReorderQuizQuestions;

public sealed class ReorderQuizQuestionsCommandValidator : AbstractValidator<ReorderQuizQuestionsCommand>
{
    public ReorderQuizQuestionsCommandValidator()
    {
        RuleFor(command => command.QuestionIds).ValidOrder();
        RuleFor(command => command.Version).ValidVersion();
    }
}
