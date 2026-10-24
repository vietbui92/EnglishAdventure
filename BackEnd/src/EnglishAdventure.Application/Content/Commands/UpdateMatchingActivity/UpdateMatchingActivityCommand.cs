using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateMatchingActivity;

/// <summary>Changes a draft lesson's matching: its mode and its 2–6 words (S3.3, PD-012).</summary>
/// <param name="Version">The lesson's version.</param>
public sealed record UpdateMatchingActivityCommand(
    Guid LessonId,
    Guid ActivityId,
    string Mode,
    IReadOnlyList<Guid> WordIds,
    string Version) : IRequest, IMatchingFields;
