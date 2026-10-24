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

    /// <summary>The lesson's words in their order, with the lesson's status and version.</summary>
    Task<LessonWordBank?> GetLessonWordBankAsync(Guid lessonId, CancellationToken cancellationToken);

    /// <summary>The lesson's activities in their order with their questions, for Admins (answers included).</summary>
    Task<LessonActivities?> GetLessonActivitiesAsync(Guid lessonId, CancellationToken cancellationToken);
}
