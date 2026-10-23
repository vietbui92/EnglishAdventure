using EnglishAdventure.Domain.Entities.Identity;

namespace EnglishAdventure.Application.Authorization;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken cancellationToken);

    void Add(Permission permission);
}
