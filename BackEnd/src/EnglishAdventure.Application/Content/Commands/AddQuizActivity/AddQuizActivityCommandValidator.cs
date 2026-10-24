using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.AddQuizActivity;

public sealed class AddQuizActivityCommandValidator : AbstractValidator<AddQuizActivityCommand>
{
    public AddQuizActivityCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
