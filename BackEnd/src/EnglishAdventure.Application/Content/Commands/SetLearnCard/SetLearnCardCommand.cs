using MediatR;

namespace EnglishAdventure.Application.Content.Commands.SetLearnCard;

/// <summary>
/// Shows or hides a word in a draft lesson's learn step (S3.7). A lesson needs at least one card to be published.
/// </summary>
/// <param name="Version">The lesson's version.</param>
public sealed record SetLearnCardCommand(Guid LessonId, Guid WordId, bool Show, string Version) : IRequest;
