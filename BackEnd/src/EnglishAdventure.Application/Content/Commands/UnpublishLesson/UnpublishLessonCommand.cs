using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UnpublishLesson;

/// <summary>
/// Turns a published lesson back into a draft, only while no child has started it (PD-012). Unpublishing a draft
/// changes nothing.
/// </summary>
public sealed record UnpublishLessonCommand(Guid LessonId, string Version) : IRequest;
