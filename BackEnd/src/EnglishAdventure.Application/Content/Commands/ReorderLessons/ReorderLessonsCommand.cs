using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderLessons;

/// <summary>
/// Puts the level's non-archived lessons in the given order. The list must name each of them exactly once; otherwise
/// the lessons changed meanwhile (409). Allowed for published lessons too: progress is never taken away (PD-002, S4.2).
/// </summary>
public sealed record ReorderLessonsCommand(Guid LevelId, IReadOnlyList<Guid> LessonIds) : IRequest;
