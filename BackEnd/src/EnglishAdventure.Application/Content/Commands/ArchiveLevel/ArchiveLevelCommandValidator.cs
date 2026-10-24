using FluentValidation;

namespace EnglishAdventure.Application.Content.Commands.ArchiveLevel;

public sealed class ArchiveLevelCommandValidator : AbstractValidator<ArchiveLevelCommand>
{
    public ArchiveLevelCommandValidator()
    {
        RuleFor(command => command.Version).ValidVersion();
    }
}
