using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UnpublishLevel;

/// <summary>
/// Turns a published level back into a draft, only while no child has started any of its lessons (PD-012).
/// Unpublishing a draft changes nothing.
/// </summary>
public sealed record UnpublishLevelCommand(Guid LevelId, string Version) : IRequest;
