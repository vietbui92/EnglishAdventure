namespace EnglishAdventure.Api.Contracts.Content;

/// <param name="Title">1 to 100 characters.</param>
/// <param name="Description">Up to 500 characters; blank means none.</param>
public sealed record CreateLevelRequest(string Title, string? Description);

/// <param name="Version">The <c>version</c> returned with the level.</param>
public sealed record UpdateLevelRequest(string Title, string? Description, string Version);

/// <param name="Title">1 to 100 characters.</param>
public sealed record CreateLessonRequest(string Title);

/// <param name="Version">The <c>version</c> returned with the lesson.</param>
public sealed record UpdateLessonRequest(string Title, string Version);

/// <param name="Version">The <c>version</c> returned with the level or lesson.</param>
public sealed record ContentVersionRequest(string Version);

/// <param name="LevelIds">Every non-archived level, once, in the new order.</param>
public sealed record ReorderLevelsRequest(IReadOnlyList<Guid> LevelIds);

/// <param name="LessonIds">Every non-archived lesson of the level, once, in the new order.</param>
public sealed record ReorderLessonsRequest(IReadOnlyList<Guid> LessonIds);
