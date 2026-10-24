using EnglishAdventure.Application.Content.Models;

namespace EnglishAdventure.Application.Content;

/// <summary>
/// What children see of the content (S3.4): published levels, lessons and bosses only, and lessons without their
/// answers. Drafts and archived items never appear.
/// </summary>
public interface IPlayQueries
{
    /// <summary>Published levels in order, each with its published lessons in order and its published boss.</summary>
    Task<LearnerMap> GetMapAsync(CancellationToken cancellationToken);

    /// <summary>A published lesson or boss of a published level, ready to play; null otherwise.</summary>
    Task<PlayLesson?> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken);
}
