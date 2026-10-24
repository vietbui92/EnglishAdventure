using EnglishAdventure.Application.Content.Models;

namespace EnglishAdventure.Application.Content;

/// <summary>Read-only views of the content for Admins, drafts and archived items included.</summary>
public interface IContentQueries
{
    /// <summary>Non-archived levels in their order, then archived ones (only when asked for) by archiving time.</summary>
    Task<IReadOnlyList<LevelSummary>> ListLevelsAsync(bool includeArchived, CancellationToken cancellationToken);

    /// <summary>The level with all its lessons, ordered like <see cref="ListLevelsAsync"/>.</summary>
    Task<LevelDetails?> GetLevelAsync(Guid levelId, CancellationToken cancellationToken);

    Task<LessonSummary?> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken);
}
