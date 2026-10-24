using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>
/// The sound a word makes (a cat's meow), which the Admin links to like a picture (S3.9, PD-013): an absolute
/// <c>https://</c> address of at most <see cref="MaxLength"/> characters. The word itself is read by the device's voice.
/// </summary>
public sealed record SoundUrl
{
    public const int MaxLength = 2000;

    private SoundUrl(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length is > 0 and <= MaxLength
               && Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && !string.IsNullOrEmpty(uri.Host);
    }

    public static SoundUrl From(string? value) =>
        IsValid(value)
            ? new SoundUrl(value!.Trim())
            : throw new DomainException($"A sound needs an https:// address of at most {MaxLength} characters.");

    /// <summary>Blank means no sound.</summary>
    public static SoundUrl? FromOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : From(value);

    public override string ToString() => Value;
}
