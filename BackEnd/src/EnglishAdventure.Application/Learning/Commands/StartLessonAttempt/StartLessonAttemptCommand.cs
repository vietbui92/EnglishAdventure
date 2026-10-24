using EnglishAdventure.Application.Learning.Models;
using MediatR;

namespace EnglishAdventure.Application.Learning.Commands.StartLessonAttempt;

/// <summary>
/// Starts playing a lesson or boss for the current child (S4.1), or resumes the child's unfinished attempt of it
/// (PD-012). The lesson and its level must be published; locks come with S4.2.
/// </summary>
public sealed record StartLessonAttemptCommand(Guid LessonId) : IRequest<AttemptView>;
