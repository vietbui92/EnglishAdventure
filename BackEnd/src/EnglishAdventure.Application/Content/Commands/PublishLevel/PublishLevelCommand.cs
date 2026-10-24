using MediatR;

namespace EnglishAdventure.Application.Content.Commands.PublishLevel;

/// <summary>Shows a draft level to learners. Publishing twice changes nothing.</summary>
public sealed record PublishLevelCommand(Guid LevelId, string Version) : IRequest;
