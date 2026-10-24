using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UpdateBoss;

public sealed class UpdateBossCommandValidator : AbstractValidator<UpdateBossCommand>
{
    public UpdateBossCommandValidator()
    {
        RuleFor(command => command.Title).ValidTitle();
        RuleFor(command => command.ImageUrl).ValidOptionalImageUrl();
        RuleFor(command => command.Version).ValidVersion();
    }
}
