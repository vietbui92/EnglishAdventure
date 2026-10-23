using EnglishAdventure.Application.Family;
using EnglishAdventure.Application.Family.Models;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Queries;

internal sealed class ChildProfileQueries : IChildProfileQueries
{
    private readonly EnglishAdventureDbContext _dbContext;

    public ChildProfileQueries(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ChildProfileSummary>> ListAsync(
        Guid parentUserId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var profiles = await _dbContext.ChildProfiles
            .AsNoTracking()
            .Where(profile => profile.ParentUserId == parentUserId)
            .Where(profile => includeArchived || profile.Status == ChildProfileStatus.Active)
            .OrderBy(profile => profile.Status)
            .ThenBy(profile => profile.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return profiles.Select(ToSummary).ToList();
    }

    public async Task<ChildProfileSummary?> GetAsync(
        Guid profileId,
        Guid parentUserId,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.ChildProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == profileId && candidate.ParentUserId == parentUserId,
                cancellationToken);

        return profile is null ? null : ToSummary(profile);
    }

    private static ChildProfileSummary ToSummary(ChildProfile profile) =>
        new(
            profile.Id,
            profile.Nickname.Value,
            profile.AvatarKey,
            profile.Status.ToString(),
            profile.CreatedAtUtc,
            profile.ArchivedAtUtc);
}
