using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>
/// A child's nickname: 1 to <see cref="MaxLength"/> characters after trimming. It is the only name stored for a child
/// (no real names, privacy principles). Within a family, nicknames that differ only in letter case count as the same
/// (the database column compares without case).
/// </summary>
public sealed record Nickname
{
    public const int MaxLength = 50;

    private Nickname(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Nickname From(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxLength)
        {
            throw new DomainException($"A nickname of 1 to {MaxLength} characters is required.");
        }

        return new Nickname(trimmed);
    }

    public override string ToString() => Value;
}
