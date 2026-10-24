using MediatR;

namespace EnglishAdventure.Application.Content.Commands.RemoveLessonWord;

/// <summary>Removes a word from a draft lesson's word bank; the words after it move up (PD-012).</summary>
/// <param name="Version">The lesson's version.</param>
public sealed record RemoveLessonWordCommand(Guid LessonId, Guid WordId, string Version) : IRequest;
