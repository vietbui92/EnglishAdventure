using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.Entities.Identity;

/// <summary>
/// One permission granted to a role. Created and removed only through <see cref="Role"/>.
/// </summary>
public sealed class RolePermission : Entity
{
    private RolePermission()
    {
    }

    internal RolePermission(Guid roleId, Guid permissionId)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }
}
