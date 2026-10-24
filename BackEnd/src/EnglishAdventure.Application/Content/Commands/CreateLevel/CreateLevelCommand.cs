using MediatR;

namespace EnglishAdventure.Application.Content.Commands.CreateLevel;

/// <summary>Adds a draft level at the end of the adventure. Returns its Id.</summary>
public sealed record CreateLevelCommand(string Title, string? Description) : IRequest<Guid>;
