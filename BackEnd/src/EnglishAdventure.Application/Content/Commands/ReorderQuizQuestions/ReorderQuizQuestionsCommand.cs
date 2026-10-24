using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderQuizQuestions;

/// <summary>Puts a draft lesson's quiz questions in the given order; a list that does not match answers 409.</summary>
/// <param name="Version">The lesson's version.</param>
public sealed record ReorderQuizQuestionsCommand(
    Guid LessonId,
    Guid ActivityId,
    IReadOnlyList<Guid> QuestionIds,
    string Version) : IRequest;
