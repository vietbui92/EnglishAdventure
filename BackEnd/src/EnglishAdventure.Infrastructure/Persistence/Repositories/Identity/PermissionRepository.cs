using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Identity;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public PermissionRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Permission>> ListAsync(CancellationToken cancellationToken) =>
        await _dbContext.Permissions.ToListAsync(cancellationToken);

    public void Add(Permission permission) => _dbContext.Permissions.Add(permission);
}
