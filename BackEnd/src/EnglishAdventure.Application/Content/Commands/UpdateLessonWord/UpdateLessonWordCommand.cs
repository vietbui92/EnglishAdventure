using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateLessonWord;

/// <summary>
/// Corrects a word's English text, meaning and picture, also while the lesson is published (PD-012). A published
/// lesson's words keep their pictures.
/// </summary>
/// <param name="Movement">How the picture moves on the discovery walk (S3.9): a <c>WordMovement</c> name; null is "Still".</param>
/// <param name="SoundUrl">The sound the word makes, if any.</param>
/// <param name="Version">The lesson's version.</param>
public sealed record UpdateLessonWordCommand(
    Guid LessonId,
    Guid WordId,
    string English,
    string Meaning,
    string? ImageUrl,
    string? Movement,
    string? SoundUrl,
    string Version) : IRequest;
