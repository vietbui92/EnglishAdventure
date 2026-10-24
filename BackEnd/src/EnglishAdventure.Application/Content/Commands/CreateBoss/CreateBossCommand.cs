using MediatR;

namespace EnglishAdventure.Application.Content.Commands.CreateBoss;

/// <summary>
/// Adds the level's boss challenge as a draft (S3.8): a name and, now or later, a picture. A level has at most one boss
/// that is not archived (422 <c>boss-exists</c>). Returns the boss lesson's Id.
/// </summary>
public sealed record CreateBossCommand(Guid LevelId, string Title, string? ImageUrl) : IRequest<Guid>;
