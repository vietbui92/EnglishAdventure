using EnglishAdventure.Application.Content;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Queries;

/// <summary>A lesson is started once a child has an attempt of it, finished or not (S4.1, PD-012).</summary>
internal sealed class LearnerActivityReader : ILearnerActivityReader
{
    private readonly EnglishAdventureDbContext _dbContext;

    public LearnerActivityReader(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasLessonBeenStartedAsync(Guid lessonId, CancellationToken cancellationToken) =>
        _dbContext.LessonAttempts.AnyAsync(attempt => attempt.LessonId == lessonId, cancellationToken);

    public Task<bool> HasAnyLessonOfLevelBeenStartedAsync(Guid levelId, CancellationToken cancellationToken) =>
        _dbContext.LessonAttempts.AnyAsync(
            attempt => _dbContext.Lessons.Any(lesson => lesson.Id == attempt.LessonId && lesson.LevelId == levelId),
            cancellationToken);
}
