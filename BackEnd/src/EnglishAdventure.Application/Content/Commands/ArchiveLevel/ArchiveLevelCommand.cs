using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ArchiveLevel;

/// <summary>
/// Retires a level for good (PD-012): it leaves the order, learners no longer see it, and its lessons are frozen.
/// Children's results are kept. Archiving twice changes nothing.
/// </summary>
public sealed record ArchiveLevelCommand(Guid LevelId, string Version) : IRequest;
