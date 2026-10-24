using EnglishAdventure.Application.Rewards;
using EnglishAdventure.Domain.Entities.Rewards;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Rewards;

internal sealed class RewardsRepository : IRewardsRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public RewardsRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetPointsAsync(Guid childProfileId, CancellationToken cancellationToken) =>
        await _dbContext.PointsTransactions
            .Where(transaction => transaction.ChildProfileId == childProfileId)
            .SumAsync(transaction => (int?)transaction.Points, cancellationToken) ?? 0;

    public async Task<IReadOnlyList<BadgeAward>> GetAwardsAsync(Guid childProfileId, CancellationToken cancellationToken) =>
        await _dbContext.BadgeAwards
            .AsNoTracking()
            .Where(award => award.ChildProfileId == childProfileId)
            .OrderBy(award => award.AwardedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BadgeDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken) =>
        await _dbContext.BadgeDefinitions.ToListAsync(cancellationToken);

    public Task<BadgeDefinition?> GetDefinitionAsync(BadgeKind kind, CancellationToken cancellationToken) =>
        _dbContext.BadgeDefinitions.SingleOrDefaultAsync(badge => badge.Kind == kind, cancellationToken);

    public void Add(PointsTransaction transaction) => _dbContext.PointsTransactions.Add(transaction);

    public void Add(BadgeAward award) => _dbContext.BadgeAwards.Add(award);
}
