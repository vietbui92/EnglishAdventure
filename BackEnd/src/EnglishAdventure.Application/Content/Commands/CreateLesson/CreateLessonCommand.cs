using MediatR;

namespace EnglishAdventure.Application.Content.Commands.CreateLesson;

/// <summary>Adds a draft lesson at the end of a level that is not archived. Returns its Id.</summary>
public sealed record CreateLessonCommand(Guid LevelId, string Title) : IRequest<Guid>;
