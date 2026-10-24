using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.AddMatchingActivity;

public sealed class AddMatchingActivityCommandValidator : AbstractValidator<AddMatchingActivityCommand>
{
    public AddMatchingActivityCommandValidator()
    {
        Include(new MatchingFieldsValidator());
        RuleFor(command => command.Version).ValidVersion();
    }
}
