using FluentValidation;

namespace EnglishAdventure.Application.Learning.Commands.MatchPair;

public sealed class MatchPairCommandValidator : AbstractValidator<MatchPairCommand>
{
    public MatchPairCommandValidator()
    {
        RuleFor(command => command.LeftId).NotEmpty();
        RuleFor(command => command.RightId).NotEmpty();
    }
}
