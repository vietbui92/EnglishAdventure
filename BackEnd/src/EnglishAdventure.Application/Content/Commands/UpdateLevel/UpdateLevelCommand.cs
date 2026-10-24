using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateLevel;

/// <summary>Changes the title and description of a draft or published level (PD-012 allows fixing texts).</summary>
public sealed record UpdateLevelCommand(Guid LevelId, string Title, string? Description, string Version) : IRequest;
