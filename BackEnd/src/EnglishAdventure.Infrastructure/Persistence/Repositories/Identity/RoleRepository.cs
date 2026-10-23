using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Identity;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public RoleRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        _dbContext.Roles
            .Include(role => role.Permissions)
            .SingleOrDefaultAsync(role => role.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> ListByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken) =>
        await _dbContext.Roles
            .Where(role => names.Contains(role.Name))
            .ToListAsync(cancellationToken);

    public void Add(Role role) => _dbContext.Roles.Add(role);
}
