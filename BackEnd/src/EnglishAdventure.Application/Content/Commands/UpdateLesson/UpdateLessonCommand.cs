using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateLesson;

/// <summary>Changes the title of a draft or published lesson (PD-012 allows fixing texts).</summary>
public sealed record UpdateLessonCommand(Guid LessonId, string Title, string Version) : IRequest;
