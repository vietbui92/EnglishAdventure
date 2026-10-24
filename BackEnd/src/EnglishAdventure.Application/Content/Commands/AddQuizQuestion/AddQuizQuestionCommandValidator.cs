using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.AddQuizQuestion;

public sealed class AddQuizQuestionCommandValidator : AbstractValidator<AddQuizQuestionCommand>
{
    public AddQuizQuestionCommandValidator()
    {
        Include(new QuizQuestionFieldsValidator());
        RuleFor(command => command.Version).ValidVersion();
    }
}
