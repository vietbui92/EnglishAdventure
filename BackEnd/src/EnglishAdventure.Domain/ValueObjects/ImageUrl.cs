using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>
/// A picture the Admin links to (PD-012: URLs first, uploads later): an absolute <c>https://</c> address of at most
/// <see cref="MaxLength"/> characters. Plain http is refused because the web app is served over https, where browsers
/// block http pictures.
/// </summary>
public sealed record ImageUrl
{
    public const int MaxLength = 2000;

    private ImageUrl(string value)
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

    public static ImageUrl From(string? value) =>
        IsValid(value)
            ? new ImageUrl(value!.Trim())
            : throw new DomainException($"A picture needs an https:// address of at most {MaxLength} characters.");

    /// <summary>Blank means no picture.</summary>
    public static ImageUrl? FromOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : From(value);

    public override string ToString() => Value;
}
