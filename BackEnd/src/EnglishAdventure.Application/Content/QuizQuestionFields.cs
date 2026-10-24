using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;
using FluentValidation;

namespace EnglishAdventure.Application.Content;

/// <summary>What adding and changing a quiz question both send (S3.2).</summary>
public interface IQuizQuestionFields
{
    /// <summary>"PictureToWord", "WordToPicture" or "FillInTheBlank".</summary>
    string Form { get; }

    Guid CorrectWordId { get; }

    IReadOnlyList<Guid> WrongWordIds { get; }

    string? Sentence { get; }
}

/// <summary>
/// The shape rules of a quiz question, checked before the lesson is loaded so a bad request answers 400 with a clear
/// message. That the words belong to the lesson is the lesson's own rule (422 <c>question-invalid</c>).
/// </summary>
public sealed class QuizQuestionFieldsValidator : AbstractValidator<IQuizQuestionFields>
{
    public QuizQuestionFieldsValidator()
    {
        RuleFor(fields => fields.Form)
            .Must(form => QuizForms.TryParse(form, out _))
            .WithMessage("The form must be PictureToWord, WordToPicture or FillInTheBlank.");

        RuleFor(fields => fields.CorrectWordId).NotEmpty().WithMessage("The question needs its correct word.");

        RuleFor(fields => fields.WrongWordIds)
            .Must(ids => ids is not null
                         && ids.Count + 1 >= QuizQuestionSpec.MinOptions
                         && ids.Count + 1 <= QuizQuestionSpec.MaxOptions)
            .WithMessage(
                $"A question has {QuizQuestionSpec.MinOptions} to {QuizQuestionSpec.MaxOptions} options: the correct word and 1 to {QuizQuestionSpec.MaxOptions - 1} wrong ones.")
            .Must((fields, ids) => ids is null
                                   || (ids.All(id => id != Guid.Empty)
                                       && ids.Distinct().Count() == ids.Count
                                       && !ids.Contains(fields.CorrectWordId)))
            .WithMessage("Each option is a different word, and the correct word is not also a wrong one.");

        When(
            fields => QuizForms.TryParse(fields.Form, out var form) && form == QuizQuestionForm.FillInTheBlank,
            () => RuleFor(fields => fields.Sentence)
                .Must(QuizQuestionSpec.IsValidSentence)
                .WithMessage(
                    $"A fill-in sentence has 1 to {QuizQuestionSpec.SentenceMaxLength} characters and exactly one gap ({QuizQuestionSpec.Gap})."));

        When(
            fields => QuizForms.TryParse(fields.Form, out var form) && form != QuizQuestionForm.FillInTheBlank,
            () => RuleFor(fields => fields.Sentence)
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage("Only a fill-in question has a sentence."));
    }
}

public static class QuizForms
{
    /// <summary>The form's name exactly as listed; numbers are not accepted.</summary>
    public static bool TryParse(string? value, out QuizQuestionForm form)
    {
        form = default;
        return !string.IsNullOrEmpty(value)
               && Enum.GetNames<QuizQuestionForm>().Contains(value, StringComparer.Ordinal)
               && Enum.TryParse(value, out form);
    }

    public static QuizQuestionSpec ToSpec(IQuizQuestionFields fields) =>
        QuizQuestionSpec.Create(
            TryParse(fields.Form, out var form) ? form : throw new FormatException("Invalid quiz question form."),
            fields.CorrectWordId,
            fields.WrongWordIds,
            fields.Sentence);
}
