using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.Entities.Identity;

/// <summary>
/// One role assigned to an account. Created and removed only through <see cref="Account"/>.
/// </summary>
public sealed class AccountRole : Entity
{
    private AccountRole()
    {
    }

    internal AccountRole(Guid accountId, Guid roleId, DateTime assignedAtUtc, Guid? assignedByAccountId)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc;
        AssignedByAccountId = assignedByAccountId;
    }

    public Guid AccountId { get; private set; }

    public Guid RoleId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    /// <summary>The account that granted the role; null for system provisioning.</summary>
    public Guid? AssignedByAccountId { get; private set; }
}
