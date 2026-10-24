using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateQuizQuestion;

/// <summary>
/// Changes a quiz question. In a draft anything can change; in a published lesson only a fill-in sentence can be
/// corrected (PD-012), so the form, the correct word and the wrong words must be sent unchanged.
/// </summary>
/// <param name="Version">The lesson's version.</param>
public sealed record UpdateQuizQuestionCommand(
    Guid LessonId,
    Guid ActivityId,
    Guid QuestionId,
    string Form,
    Guid CorrectWordId,
    IReadOnlyList<Guid> WrongWordIds,
    string? Sentence,
    string Version) : IRequest, IQuizQuestionFields;
