using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddQuizQuestion;

/// <summary>
/// Adds a question at the end of a draft lesson's quiz (S3.2). The correct word and the 1–3 wrong ones come from the
/// lesson's word bank. Returns the question's Id.
/// </summary>
/// <param name="Version">The lesson's version.</param>
public sealed record AddQuizQuestionCommand(
    Guid LessonId,
    Guid ActivityId,
    string Form,
    Guid CorrectWordId,
    IReadOnlyList<Guid> WrongWordIds,
    string? Sentence,
    string Version) : IRequest<Guid>, IQuizQuestionFields;
