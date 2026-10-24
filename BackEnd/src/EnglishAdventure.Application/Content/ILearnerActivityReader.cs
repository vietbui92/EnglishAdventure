namespace EnglishAdventure.Application.Content;

/// <summary>
/// Whether children have started content, which decides if it may still be unpublished (PD-012). Learning data
/// arrives with lesson attempts (S4.1); until then nothing has been started.
/// </summary>
public interface ILearnerActivityReader
{
    Task<bool> HasLessonBeenStartedAsync(Guid lessonId, CancellationToken cancellationToken);

    Task<bool> HasAnyLessonOfLevelBeenStartedAsync(Guid levelId, CancellationToken cancellationToken);
}
