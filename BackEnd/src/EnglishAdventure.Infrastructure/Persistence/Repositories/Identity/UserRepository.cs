using EnglishAdventure.Application.Users;
using EnglishAdventure.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence.Repositories.Identity;

internal sealed class UserRepository : IUserRepository
{
    private readonly EnglishAdventureDbContext _dbContext;

    public UserRepository(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Users
            .Include(user => user.Roles)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<bool> AnyOtherActiveUserWithRoleAsync(
        Guid roleId,
        Guid excludedUserId,
        CancellationToken cancellationToken) =>
        _dbContext.Users.AnyAsync(
            user => user.Id != excludedUserId
                       && user.IsActive
                       && user.Roles.Any(userRole => userRole.RoleId == roleId),
            cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        return _dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> AnyActiveUserWithRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
        _dbContext.Users.AnyAsync(
            user => user.IsActive && user.Roles.Any(userRole => userRole.RoleId == roleId),
            cancellationToken);

    public void Add(User user) => _dbContext.Users.Add(user);
}
