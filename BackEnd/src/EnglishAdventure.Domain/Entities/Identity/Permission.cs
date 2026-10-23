using System.Text.RegularExpressions;
using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Identity;

/// <summary>
/// One specific action that can be authorized, identified by a stable code such as <c>content.manage</c>.
/// </summary>
public sealed partial class Permission : Entity
{
    public const int CodeMaxLength = 100;
    public const int DescriptionMaxLength = 256;

    private Permission()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public static Permission Create(string code, string? description = null)
    {
        if (code is null || code.Length > CodeMaxLength || !CodePattern().IsMatch(code))
        {
            throw new DomainException(
                $"Permission code '{code}' must look like '<area>.<action>' in lower case, for example 'content.manage'.");
        }

        EnsureValidDescription(description);

        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Description = description
        };
    }

    public void Describe(string? description)
    {
        EnsureValidDescription(description);
        Description = description;
    }

    private static void EnsureValidDescription(string? description)
    {
        if (description?.Length > DescriptionMaxLength)
        {
            throw new DomainException($"A permission description cannot exceed {DescriptionMaxLength} characters.");
        }
    }

    // Lower-case words joined by '-', at least two segments joined by '.': "content.read-drafts".
    [GeneratedRegex(@"^[a-z]+(-[a-z]+)*(\.[a-z]+(-[a-z]+)*)+$")]
    private static partial Regex CodePattern();
}
