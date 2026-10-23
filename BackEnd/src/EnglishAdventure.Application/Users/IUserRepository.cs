using EnglishAdventure.Domain.Entities.Identity;

namespace EnglishAdventure.Application.Users;

public interface IUserRepository
{
    /// <summary>Loads the user with its role assignments, or null.</summary>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Loads the user with this email (any letter case) and its role assignments, or null.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<bool> AnyActiveUserWithRoleAsync(Guid roleId, CancellationToken cancellationToken);

    /// <summary>Whether an active user other than <paramref name="excludedUserId"/> holds the role.</summary>
    Task<bool> AnyOtherActiveUserWithRoleAsync(Guid roleId, Guid excludedUserId, CancellationToken cancellationToken);

    void Add(User user);
}
