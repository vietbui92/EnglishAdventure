using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddLessonWord;

/// <summary>
/// Adds a word at the end of a draft lesson's word bank (S3.6). The picture can come later, but is required before the
/// lesson is published. Returns the word's Id.
/// </summary>
/// <param name="Movement">How the picture moves on the discovery walk (S3.9): a <c>WordMovement</c> name; null is "Still".</param>
/// <param name="SoundUrl">The sound the word makes, if any.</param>
/// <param name="Version">The lesson's version.</param>
public sealed record AddLessonWordCommand(
    Guid LessonId,
    string English,
    string Meaning,
    string? ImageUrl,
    string? Movement,
    string? SoundUrl,
    string Version) : IRequest<Guid>;
