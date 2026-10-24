using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateBoss;

/// <summary>Changes the boss's name and picture, also while published; a published boss keeps a picture (S3.8).</summary>
/// <param name="Version">The boss lesson's version.</param>
public sealed record UpdateBossCommand(Guid LessonId, string Title, string? ImageUrl, string Version) : IRequest;
