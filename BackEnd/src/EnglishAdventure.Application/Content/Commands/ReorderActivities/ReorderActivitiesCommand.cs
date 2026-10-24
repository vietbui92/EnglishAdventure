using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderActivities;

/// <summary>Puts a draft lesson's activities in the given order; a list that does not match answers 409.</summary>
/// <param name="Version">The lesson's version.</param>
public sealed record ReorderActivitiesCommand(Guid LessonId, IReadOnlyList<Guid> ActivityIds, string Version) : IRequest;
