using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>The title of a level or lesson: 1 to <see cref="MaxLength"/> characters after trimming.</summary>
public sealed record ContentTitle
{
    public const int MaxLength = 100;

    private ContentTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ContentTitle From(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxLength)
        {
            throw new DomainException($"A title of 1 to {MaxLength} characters is required.");
        }

        return new ContentTitle(trimmed);
    }

    public override string ToString() => Value;
}
