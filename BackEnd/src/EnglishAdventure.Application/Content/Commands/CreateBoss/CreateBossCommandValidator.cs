using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.CreateBoss;

public sealed class CreateBossCommandValidator : AbstractValidator<CreateBossCommand>
{
    public CreateBossCommandValidator()
    {
        RuleFor(command => command.Title).ValidTitle();
        RuleFor(command => command.ImageUrl).ValidOptionalImageUrl();
    }
}
