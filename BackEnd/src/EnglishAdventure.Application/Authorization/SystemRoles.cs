using static EnglishAdventure.Application.Authorization.Permissions;

namespace EnglishAdventure.Application.Authorization;

/// <summary>
/// The three built-in roles decided in PD-008 and their default permissions. They are created and synced by the
/// bootstrap command (S1.2). More roles can be created at runtime by holders of <see cref="Permissions.Roles.Manage"/>.
/// </summary>
public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string ContentAdmin = "ContentAdmin";
    public const string Parent = "Parent";

    public static IReadOnlyList<SystemRoleDefinition> All { get; } =
    [
        // SuperAdmin always holds every permission in the catalog, except the Family ones: full administration
        // does not include reading families' learning records (PD-008, least privilege).
        new(
            SuperAdmin,
            "Full system administration, including users, roles and permissions.",
            Permissions.All
                .Select(permission => permission.Code)
                .Where(code => !code.StartsWith("family.", StringComparison.Ordinal))
                .ToArray()),
        new(
            ContentAdmin,
            "Creates, edits, orders and publishes learning content.",
            [Content.ReadDrafts, Content.Manage, Content.Publish]),
        new(
            Parent,
            "Manages own children's learning profiles and views their progress.",
            [Family.ManageChildren, Family.ReadProgress])
    ];
}

public sealed record SystemRoleDefinition(string Name, string Description, IReadOnlyList<string> Permissions);
