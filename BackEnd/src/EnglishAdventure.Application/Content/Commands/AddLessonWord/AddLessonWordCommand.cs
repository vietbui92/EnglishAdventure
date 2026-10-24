using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddLessonWord;

/// <summary>
/// Adds a word at the end of a draft lesson's word bank (S3.6). The picture can come later, but is required before the
/// lesson is published. Returns the word's Id.
/// </summary>
/// <param name="Version">The lesson's version.</param>
public sealed record AddLessonWordCommand(
    Guid LessonId,
    string English,
    string Meaning,
    string? ImageUrl,
    string Version) : IRequest<Guid>;
