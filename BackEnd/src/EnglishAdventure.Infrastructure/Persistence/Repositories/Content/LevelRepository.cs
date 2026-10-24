using EnglishAdventure.Application.Content;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Infrastructure.Persistence.Configurations.Content;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Content;

internal sealed class LevelRepository : ILevelRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public LevelRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Level?> GetAsync(Guid levelId, CancellationToken cancellationToken) =>
        _dbContext.Levels.SingleOrDefaultAsync(level => level.Id == levelId, cancellationToken);

    public async Task<IReadOnlyList<Level>> ListInOrderAsync(CancellationToken cancellationToken) =>
        await _dbContext.Levels
            .Where(level => level.Status != ContentStatus.Archived)
            .OrderBy(level => level.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<int> MaxSortOrderAsync(CancellationToken cancellationToken) =>
        await _dbContext.Levels
            .Where(level => level.Status != ContentStatus.Archived)
            .MaxAsync(level => (int?)level.SortOrder, cancellationToken) ?? 0;

    public void Add(Level level) => _dbContext.Levels.Add(level);

    public void ExpectVersion(Level level, byte[] version) =>
        _dbContext.Entry(level).Property<byte[]>(OrderedContentMapping.RowVersion).OriginalValue = version;
}
