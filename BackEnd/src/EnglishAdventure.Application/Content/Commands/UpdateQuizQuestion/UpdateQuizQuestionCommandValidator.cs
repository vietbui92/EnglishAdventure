using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UpdateQuizQuestion;

public sealed class UpdateQuizQuestionCommandValidator : AbstractValidator<UpdateQuizQuestionCommand>
{
    public UpdateQuizQuestionCommandValidator()
    {
        Include(new QuizQuestionFieldsValidator());
        RuleFor(command => command.Version).ValidVersion();
    }
}
