using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>
/// What a quiz question asks (S3.2): its form, the word that answers it, the wrong options chosen by the Admin
/// (owner, 2026-10-24) and, for a gap sentence, the sentence. All words come from the lesson's word bank, which the
/// lesson checks. 2 to 4 options in all, so exactly one of them is correct (PD-012).
/// </summary>
public sealed record QuizQuestionSpec
{
    public const int MinOptions = 2;
    public const int MaxOptions = 4;
    public const int SentenceMaxLength = 200;

    /// <summary>The gap in a fill-in sentence, where the answer goes.</summary>
    public const string Gap = "___";

    private QuizQuestionSpec(QuizQuestionForm form, Guid correctWordId, IReadOnlyList<Guid> wrongWordIds, string? sentence)
    {
        Form = form;
        CorrectWordId = correctWordId;
        WrongWordIds = wrongWordIds;
        Sentence = sentence;
    }

    public QuizQuestionForm Form { get; }

    public Guid CorrectWordId { get; }

    public IReadOnlyList<Guid> WrongWordIds { get; }

    /// <summary>Only for <see cref="QuizQuestionForm.FillInTheBlank"/>; it has exactly one <see cref="Gap"/>.</summary>
    public string? Sentence { get; }

    public static QuizQuestionSpec Create(
        QuizQuestionForm form,
        Guid correctWordId,
        IReadOnlyList<Guid> wrongWordIds,
        string? sentence)
    {
        if (!Enum.IsDefined(form))
        {
            throw Invalid("Unknown question form.");
        }

        if (correctWordId == Guid.Empty || wrongWordIds.Any(id => id == Guid.Empty))
        {
            throw Invalid("Every option needs a word.");
        }

        var options = wrongWordIds.Count + 1;
        if (options < MinOptions || options > MaxOptions)
        {
            throw Invalid($"A question has {MinOptions} to {MaxOptions} options.");
        }

        if (wrongWordIds.Contains(correctWordId) || wrongWordIds.Distinct().Count() != wrongWordIds.Count)
        {
            throw Invalid("Each option is a different word, and only one of them is correct.");
        }

        return new QuizQuestionSpec(form, correctWordId, wrongWordIds.ToList(), ValidSentence(form, sentence));
    }

    /// <summary>1 to <see cref="SentenceMaxLength"/> characters after trimming, with exactly one <see cref="Gap"/>.</summary>
    public static bool IsValidSentence(string? sentence)
    {
        var trimmed = sentence?.Trim() ?? string.Empty;
        var first = trimmed.IndexOf(Gap, StringComparison.Ordinal);
        return trimmed.Length > 0
               && trimmed.Length <= SentenceMaxLength
               && first >= 0
               && first == trimmed.LastIndexOf(Gap, StringComparison.Ordinal)
               && !trimmed.Contains(Gap + "_", StringComparison.Ordinal);
    }

    /// <summary>Whether two specs ask the same thing with the same options; only the sentence text may differ.</summary>
    public bool HasSameStructureAs(QuizQuestionSpec other) =>
        Form == other.Form
        && CorrectWordId == other.CorrectWordId
        && WrongWordIds.ToHashSet().SetEquals(other.WrongWordIds);

    private static string? ValidSentence(QuizQuestionForm form, string? sentence)
    {
        if (form != QuizQuestionForm.FillInTheBlank)
        {
            return string.IsNullOrWhiteSpace(sentence)
                ? null
                : throw Invalid("Only a fill-in question has a sentence.");
        }

        return IsValidSentence(sentence)
            ? sentence!.Trim()
            : throw Invalid($"A fill-in sentence has 1 to {SentenceMaxLength} characters and exactly one gap ({Gap}).");
    }

    private static DomainException Invalid(string message) => new(message, ContentErrorCodes.InvalidQuestion);
}
