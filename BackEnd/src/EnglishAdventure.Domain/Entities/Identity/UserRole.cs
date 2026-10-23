using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.Entities.Identity;

/// <summary>
/// One role assigned to a user. Created and removed only through <see cref="User"/>.
/// </summary>
public sealed class UserRole : Entity
{
    private UserRole()
    {
    }

    internal UserRole(Guid userId, Guid roleId, DateTime assignedAtUtc, Guid? assignedByUserId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc;
        AssignedByUserId = assignedByUserId;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    /// <summary>The user that granted the role; null for system provisioning.</summary>
    public Guid? AssignedByUserId { get; private set; }
}
