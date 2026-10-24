using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateLessonWord;

/// <summary>
/// Corrects a word's English text, meaning and picture, also while the lesson is published (PD-012). A published
/// lesson's words keep their pictures.
/// </summary>
/// <param name="Version">The lesson's version.</param>
public sealed record UpdateLessonWordCommand(
    Guid LessonId,
    Guid WordId,
    string English,
    string Meaning,
    string? ImageUrl,
    string Version) : IRequest;
