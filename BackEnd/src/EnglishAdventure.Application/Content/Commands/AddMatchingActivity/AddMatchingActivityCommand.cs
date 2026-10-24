using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddMatchingActivity;

/// <summary>
/// Adds a matching after a draft lesson's other activities (S3.3): 2–6 words of the word bank, matched with their
/// pictures or meanings. Returns its Id.
/// </summary>
/// <param name="Version">The lesson's version.</param>
public sealed record AddMatchingActivityCommand(
    Guid LessonId,
    string Mode,
    IReadOnlyList<Guid> WordIds,
    string Version) : IRequest<Guid>, IMatchingFields;
