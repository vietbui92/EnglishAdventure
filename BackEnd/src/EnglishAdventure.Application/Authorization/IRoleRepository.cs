using EnglishAdventure.Domain.Entities.Identity;

namespace EnglishAdventure.Application.Authorization;

public interface IRoleRepository
{
    /// <summary>Loads the role with its permission grants, or null.</summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Loads the roles with these names (without their grants). Unknown names are simply missing.</summary>
    Task<IReadOnlyList<Role>> ListByNamesAsync(IReadOnlyCollection<string> names, CancellationToken cancellationToken);

    void Add(Role role);
}
