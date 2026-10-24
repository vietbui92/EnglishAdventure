using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One word of a lesson's word bank (S3.6, PD-011): the English word, its Vietnamese meaning and a picture. The learn
/// step, quiz and matching activities are built from these words. Owned by <see cref="Lesson"/>, which changes it.
/// </summary>
public sealed class LessonWord : Entity
{
    public const int EnglishMaxLength = 50;
    public const int MeaningMaxLength = 100;

    private LessonWord()
    {
    }

    public Guid LessonId { get; private set; }

    /// <summary>The position in the word bank, starting at 1.</summary>
    public int SortOrder { get; private set; }

    public string English { get; private set; } = string.Empty;

    /// <summary>In Vietnamese, for the learn step's word cards.</summary>
    public string Meaning { get; private set; } = string.Empty;

    /// <summary>Optional while drafting; every word needs one before the lesson is published (owner, 2026-10-24).</summary>
    public ImageUrl? ImageUrl { get; private set; }

    /// <summary>Whether the word is one of the learn step's cards (S3.7). New words are.</summary>
    public bool ShowInLearnStep { get; private set; }

    internal static LessonWord Create(Guid lessonId, int sortOrder, string english, string meaning, ImageUrl? imageUrl)
    {
        var word = new LessonWord { Id = Guid.NewGuid(), LessonId = lessonId, SortOrder = sortOrder, ShowInLearnStep = true };
        word.Change(english, meaning, imageUrl);
        return word;
    }

    /// <summary>The English word as compared for duplicates: trimmed, any letter case.</summary>
    internal static string Normalize(string? english) => english?.Trim().ToUpperInvariant() ?? string.Empty;

    internal void Change(string english, string meaning, ImageUrl? imageUrl)
    {
        English = ValidText(english, EnglishMaxLength, "An English word");
        Meaning = ValidText(meaning, MeaningMaxLength, "A meaning");
        ImageUrl = imageUrl;
    }

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;

    internal void ShowInLearnStepAs(bool show) => ShowInLearnStep = show;

    private static string ValidText(string? value, int maxLength, string what)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length > 0 && trimmed.Length <= maxLength
            ? trimmed
            : throw new DomainException($"{what} of 1 to {maxLength} characters is required.");
    }
}
