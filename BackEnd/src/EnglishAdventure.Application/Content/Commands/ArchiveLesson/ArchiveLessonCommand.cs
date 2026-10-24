using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ArchiveLesson;

/// <summary>
/// Retires a lesson for good (PD-012): it leaves the order and learners no longer see it; children's results are
/// kept. Archiving twice changes nothing.
/// </summary>
public sealed record ArchiveLessonCommand(Guid LessonId, string Version) : IRequest;
