using EnglishAdventure.Application.Learning;
using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Learning;

internal sealed class LessonAttemptRepository : ILessonAttemptRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public LessonAttemptRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LessonAttempt?> GetAsync(Guid attemptId, CancellationToken cancellationToken) =>
        _dbContext.LessonAttempts
            .Include(attempt => attempt.Answers)
            .SingleOrDefaultAsync(attempt => attempt.Id == attemptId, cancellationToken);

    public Task<LessonAttempt?> GetInProgressAsync(Guid childProfileId, Guid lessonId, CancellationToken cancellationToken) =>
        _dbContext.LessonAttempts
            .Include(attempt => attempt.Answers)
            .SingleOrDefaultAsync(
                attempt => attempt.ChildProfileId == childProfileId
                           && attempt.LessonId == lessonId
                           && attempt.Status == AttemptStatus.InProgress,
                cancellationToken);

    public void Add(LessonAttempt attempt) => _dbContext.LessonAttempts.Add(attempt);
}
