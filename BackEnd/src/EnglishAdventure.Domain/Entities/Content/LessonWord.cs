using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One word of a lesson's word bank (S3.6, PD-011): the English word, its Vietnamese meaning, a picture, and how the
/// child meets it on the discovery walk (S3.9, PD-013). The learn step, quiz and matching activities are built from
/// these words. Owned by <see cref="Lesson"/>, which changes it.
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

    /// <summary>How the picture moves on the discovery walk (S3.9). Still unless the Admin picks another.</summary>
    public WordMovement Movement { get; private set; } = WordMovement.Still;

    /// <summary>The sound the word makes (a meow), if any. The word itself is read by the device's voice (PD-013).</summary>
    public SoundUrl? SoundUrl { get; private set; }

    /// <summary>Whether the word is one of the learn step's cards (S3.7). New words are.</summary>
    public bool ShowInLearnStep { get; private set; }

    internal static LessonWord Create(
        Guid lessonId,
        int sortOrder,
        string english,
        string meaning,
        ImageUrl? imageUrl,
        WordDiscovery discovery)
    {
        var word = new LessonWord { Id = Guid.NewGuid(), LessonId = lessonId, SortOrder = sortOrder, ShowInLearnStep = true };
        word.Change(english, meaning, imageUrl, discovery);
        return word;
    }

    /// <summary>The English word as compared for duplicates: trimmed, any letter case.</summary>
    internal static string Normalize(string? english) => english?.Trim().ToUpperInvariant() ?? string.Empty;

    internal void Change(string english, string meaning, ImageUrl? imageUrl, WordDiscovery discovery)
    {
        if (!Enum.IsDefined(discovery.Movement))
        {
            throw new DomainException("Unknown movement.");
        }

        English = ValidText(english, EnglishMaxLength, "An English word");
        Meaning = ValidText(meaning, MeaningMaxLength, "A meaning");
        ImageUrl = imageUrl;
        Movement = discovery.Movement;
        SoundUrl = discovery.Sound;
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
