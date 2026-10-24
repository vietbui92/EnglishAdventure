using EnglishAdventure.Application.Learning;
using EnglishAdventure.Domain.Entities.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Learning;

internal sealed class LessonCompletionRepository : ILessonCompletionRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public LessonCompletionRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LessonCompletion?> GetAsync(Guid childProfileId, Guid lessonId, CancellationToken cancellationToken) =>
        _dbContext.LessonCompletions.SingleOrDefaultAsync(
            completion => completion.ChildProfileId == childProfileId && completion.LessonId == lessonId,
            cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetBestStarsAsync(
        Guid childProfileId,
        CancellationToken cancellationToken) =>
        await _dbContext.LessonCompletions
            .AsNoTracking()
            .Where(completion => completion.ChildProfileId == childProfileId)
            .ToDictionaryAsync(completion => completion.LessonId, completion => completion.BestStars, cancellationToken);

    public void Add(LessonCompletion completion) => _dbContext.LessonCompletions.Add(completion);
}
