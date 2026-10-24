using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.RemoveQuizQuestion;

public sealed class RemoveQuizQuestionCommandValidator : AbstractValidator<RemoveQuizQuestionCommand>
{
    public RemoveQuizQuestionCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
