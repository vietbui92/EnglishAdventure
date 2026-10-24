using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderLessonWords;

/// <summary>
/// Puts a draft lesson's words in the given order, which must name each word once (PD-012). The lesson's version
/// catches a list that was built from an older word bank (409).
/// </summary>
/// <param name="Version">The lesson's version.</param>
public sealed record ReorderLessonWordsCommand(Guid LessonId, IReadOnlyList<Guid> WordIds, string Version) : IRequest;
