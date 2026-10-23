using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Identity;

/// <summary>
/// A named bundle of permissions (for example ContentAdmin). Code authorizes by permission, never by role name.
/// </summary>
public sealed class Role : Entity
{
    public const int NameMaxLength = 64;
    public const int DescriptionMaxLength = 256;

    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public static Role Create(string name, string? description = null)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0 || trimmedName.Length > NameMaxLength)
        {
            throw new DomainException($"A role name of 1 to {NameMaxLength} characters is required.");
        }

        if (description?.Length > DescriptionMaxLength)
        {
            throw new DomainException($"A role description cannot exceed {DescriptionMaxLength} characters.");
        }

        return new Role
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Description = description
        };
    }

    public bool HasPermission(Guid permissionId) =>
        _permissions.Any(permission => permission.PermissionId == permissionId);

    /// <summary>Granting a permission the role already has changes nothing.</summary>
    public void GrantPermission(Guid permissionId)
    {
        if (HasPermission(permissionId))
        {
            return;
        }

        _permissions.Add(new RolePermission(Id, permissionId));
    }

    public void RevokePermission(Guid permissionId) =>
        _permissions.RemoveAll(permission => permission.PermissionId == permissionId);
}
