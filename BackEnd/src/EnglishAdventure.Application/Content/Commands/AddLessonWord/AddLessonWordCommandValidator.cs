using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.AddLessonWord;

public sealed class AddLessonWordCommandValidator : AbstractValidator<AddLessonWordCommand>
{
    public AddLessonWordCommandValidator()
    {
        RuleFor(command => command.English).ValidEnglishWord();
        RuleFor(command => command.Meaning).ValidMeaning();
        RuleFor(command => command.ImageUrl).ValidOptionalImageUrl();
        RuleFor(command => command.Version).ValidVersion();
    }
}
