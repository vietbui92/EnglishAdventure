using MediatR;

namespace EnglishAdventure.Application.Content.Commands.RemoveQuizQuestion;

/// <summary>Removes a question from a draft lesson's quiz; the questions after it move up (PD-012).</summary>
/// <param name="Version">The lesson's version.</param>
public sealed record RemoveQuizQuestionCommand(Guid LessonId, Guid ActivityId, Guid QuestionId, string Version) : IRequest;
