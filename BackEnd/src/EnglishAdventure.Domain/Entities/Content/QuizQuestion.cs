using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// One quiz question (S3.2). It points to words of the lesson's word bank instead of copying their texts, so a
/// corrected word shows corrected everywhere. Exactly one option is correct: <see cref="CorrectWordId"/>.
/// </summary>
public sealed class QuizQuestion : Entity
{
    private readonly List<QuizOption> _options = new();

    private QuizQuestion()
    {
    }

    public Guid ActivityId { get; private set; }

    public int SortOrder { get; private set; }

    public QuizQuestionForm Form { get; private set; }

    public Guid CorrectWordId { get; private set; }

    /// <summary>Only for <see cref="QuizQuestionForm.FillInTheBlank"/>.</summary>
    public string? Sentence { get; private set; }

    /// <summary>The options as stored: the correct word first. Never sent to children in this order (S3.4).</summary>
    public IReadOnlyList<QuizOption> Options => _options.OrderBy(option => option.SortOrder).ToList();

    public QuizQuestionSpec Spec =>
        QuizQuestionSpec.Create(
            Form,
            CorrectWordId,
            Options.Where(option => option.WordId != CorrectWordId).Select(option => option.WordId).ToList(),
            Sentence);

    internal static QuizQuestion Create(Guid activityId, int sortOrder, QuizQuestionSpec spec)
    {
        var question = new QuizQuestion { Id = Guid.NewGuid(), ActivityId = activityId, SortOrder = sortOrder };
        question.Change(spec);
        return question;
    }

    /// <summary>Replaces what the question asks. Options are recreated from the spec.</summary>
    internal void Change(QuizQuestionSpec spec)
    {
        Form = spec.Form;
        CorrectWordId = spec.CorrectWordId;
        Sentence = spec.Sentence;

        var wordIds = new[] { spec.CorrectWordId }.Concat(spec.WrongWordIds).ToList();
        _options.RemoveAll(option => !wordIds.Contains(option.WordId));
        for (var index = 0; index < wordIds.Count; index++)
        {
            var existing = _options.SingleOrDefault(option => option.WordId == wordIds[index]);
            if (existing is null)
            {
                _options.Add(QuizOption.Create(Id, wordIds[index], index + 1));
            }
            else
            {
                existing.MoveTo(index + 1);
            }
        }
    }

    /// <summary>Only the sentence text changes (PD-012 allows fixing texts while published).</summary>
    internal void CorrectSentence(string? sentence) => Sentence = sentence;

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;

    internal bool UsesWord(Guid wordId) => _options.Any(option => option.WordId == wordId);

    public bool IsCorrect(QuizOption option) => option.WordId == CorrectWordId;
}
