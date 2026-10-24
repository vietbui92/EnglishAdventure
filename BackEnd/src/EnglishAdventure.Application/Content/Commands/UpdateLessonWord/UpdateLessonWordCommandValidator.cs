using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.UpdateLessonWord;

public sealed class UpdateLessonWordCommandValidator : AbstractValidator<UpdateLessonWordCommand>
{
    public UpdateLessonWordCommandValidator()
    {
        RuleFor(command => command.English).ValidEnglishWord();
        RuleFor(command => command.Meaning).ValidMeaning();
        RuleFor(command => command.ImageUrl).ValidOptionalImageUrl();
        RuleFor(command => command.Movement).ValidOptionalMovement();
        RuleFor(command => command.SoundUrl).ValidOptionalSoundUrl();
        RuleFor(command => command.Version).ValidVersion();
    }
}
