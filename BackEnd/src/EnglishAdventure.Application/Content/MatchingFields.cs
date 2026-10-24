using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using FluentValidation;

namespace EnglishAdventure.Application.Content;

/// <summary>What adding and changing a matching both send (S3.3).</summary>
public interface IMatchingFields
{
    /// <summary>"WordToPicture" or "WordToMeaning".</summary>
    string Mode { get; }

    IReadOnlyList<Guid> WordIds { get; }
}

/// <summary>
/// The shape rules of a matching, checked before the lesson is loaded (400). That the words belong to the lesson is the
/// lesson's own rule (422 <c>matching-invalid</c>).
/// </summary>
public sealed class MatchingFieldsValidator : AbstractValidator<IMatchingFields>
{
    public MatchingFieldsValidator()
    {
        RuleFor(fields => fields.Mode)
            .Must(mode => MatchingModes.TryParse(mode, out _))
            .WithMessage("The mode must be WordToPicture or WordToMeaning.");

        RuleFor(fields => fields.WordIds)
            .Must(ids => ids is not null
                         && ids.Count >= MatchingActivity.MinPairs
                         && ids.Count <= MatchingActivity.MaxPairs)
            .WithMessage($"A matching has {MatchingActivity.MinPairs} to {MatchingActivity.MaxPairs} words.")
            .Must(ids => ids is null || (ids.All(id => id != Guid.Empty) && ids.Distinct().Count() == ids.Count))
            .WithMessage("Each pair is a different word.");
    }
}

public static class MatchingModes
{
    /// <summary>The mode's name exactly as listed; numbers are not accepted.</summary>
    public static bool TryParse(string? value, out MatchingMode mode)
    {
        mode = default;
        return !string.IsNullOrEmpty(value)
               && Enum.GetNames<MatchingMode>().Contains(value, StringComparer.Ordinal)
               && Enum.TryParse(value, out mode);
    }

    public static MatchingMode Parse(string value) =>
        TryParse(value, out var mode) ? mode : throw new FormatException("Invalid matching mode.");
}
