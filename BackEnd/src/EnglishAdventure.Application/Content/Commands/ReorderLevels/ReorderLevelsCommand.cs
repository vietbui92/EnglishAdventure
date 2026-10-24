using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderLevels;

/// <summary>
/// Puts the non-archived levels in the given order. The list must name each of them exactly once; otherwise the
/// levels changed meanwhile (409). Allowed for published levels too: progress is never taken away (PD-002, S4.2).
/// </summary>
public sealed record ReorderLevelsCommand(IReadOnlyList<Guid> LevelIds) : IRequest;
