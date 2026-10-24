using EnglishAdventure.Domain.Entities.Learning;

namespace EnglishAdventure.Application.Learning;

public interface ILessonCompletionRepository
{
    Task<LessonCompletion?> GetAsync(Guid childProfileId, Guid lessonId, CancellationToken cancellationToken);

    /// <summary>The child's best stars per completed lesson or boss.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetBestStarsAsync(Guid childProfileId, CancellationToken cancellationToken);

    void Add(LessonCompletion completion);
}
