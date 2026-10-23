using EnglishAdventure.Application.Family;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Family;

internal sealed class ChildProfileRepository : IChildProfileRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public ChildProfileRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ChildProfile?> GetOwnedAsync(Guid profileId, Guid parentUserId, CancellationToken cancellationToken) =>
        _dbContext.ChildProfiles.SingleOrDefaultAsync(
            profile => profile.Id == profileId && profile.ParentUserId == parentUserId,
            cancellationToken);

    public Task<int> CountActiveAsync(Guid parentUserId, CancellationToken cancellationToken) =>
        _dbContext.ChildProfiles.CountAsync(
            profile => profile.ParentUserId == parentUserId && profile.Status == ChildProfileStatus.Active,
            cancellationToken);

    // The Nickname column compares without letter case (see ChildProfileConfiguration).
    public Task<bool> NicknameInUseAsync(
        Guid parentUserId,
        Nickname nickname,
        Guid? exceptProfileId,
        CancellationToken cancellationToken) =>
        _dbContext.ChildProfiles.AnyAsync(
            profile => profile.ParentUserId == parentUserId
                       && profile.Status == ChildProfileStatus.Active
                       && profile.Nickname == nickname
                       && profile.Id != exceptProfileId,
            cancellationToken);

    public void Add(ChildProfile profile) => _dbContext.ChildProfiles.Add(profile);
}
