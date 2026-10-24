using FluentValidation;

namespace EnglishAdventure.Application.Learning.Commands.AnswerQuestion;

public sealed class AnswerQuestionCommandValidator : AbstractValidator<AnswerQuestionCommand>
{
    public AnswerQuestionCommandValidator()
    {
        RuleFor(command => command.QuestionId).NotEmpty();
        RuleFor(command => command.OptionId).NotEmpty();
    }
}
