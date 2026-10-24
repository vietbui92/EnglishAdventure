using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UpdateMatchingActivity;

public sealed class UpdateMatchingActivityCommandValidator : AbstractValidator<UpdateMatchingActivityCommand>
{
    public UpdateMatchingActivityCommandValidator()
    {
        Include(new MatchingFieldsValidator());
        RuleFor(command => command.Version).ValidVersion();
    }
}
