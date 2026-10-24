namespace EnglishAdventure.Application.Authorization;

/// <summary>
/// The permission catalog. Each code is checked by code somewhere (an <c>[Authorize(Policy = ...)]</c> attribute), so
/// permissions are defined here and synced into the database; they are never created at runtime. Roles combine them.
/// To add one: add a constant, add it to <see cref="All"/>, use it in a policy, and grant it to roles.
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string Read = "users.read";
        public const string Manage = "users.manage";
    }

    public static class Roles
    {
        public const string Manage = "roles.manage";
    }

    public static class Content
    {
        public const string ReadDrafts = "content.read-drafts";
        public const string Manage = "content.manage";
        public const string Publish = "content.publish";
    }

    public static class Rewards
    {
        public const string Manage = "rewards.manage";
    }

    public static class Family
    {
        public const string ManageChildren = "family.children.manage";
        public const string ReadProgress = "family.progress.read";
    }

    public static class Learning
    {
        /// <summary>Learn as one child profile. Only a child session holds it (see <see cref="SessionOnly"/>).</summary>
        public const string Play = "learning.play";
    }

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(Users.Read, "List and view users."),
        new(Users.Manage, "Create Admin and Parent users; deactivate and reactivate users."),
        new(Roles.Manage, "Create and edit roles, choose their permissions, and assign roles to users."),
        new(Content.ReadDrafts, "View unpublished levels, lessons and activities."),
        new(Content.Manage, "Create, edit and order levels, lessons, quiz activities and matching activities."),
        new(Content.Publish, "Publish, unpublish and archive content."),
        new(Rewards.Manage, "Create and edit badge definitions."),
        new(Family.ManageChildren, "Create, rename and archive own child profiles."),
        new(Family.ReadProgress, "View own children's progress, activity results, points and badges.")
    ];

    /// <summary>
    /// Permissions that come from the kind of session, not from roles: they are not in <see cref="All"/>, so they are
    /// never synced to the database and no role, not even SuperAdmin, can hold them. A child session holds exactly
    /// these (PD-010).
    /// </summary>
    public static IReadOnlyList<string> SessionOnly { get; } = [Learning.Play];

    private static readonly HashSet<string> Codes = All.Select(permission => permission.Code)
        .Concat(SessionOnly)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>Whether the code can be used as a policy: a catalog permission or a session-only one.</summary>
    public static bool IsDefined(string code) => Codes.Contains(code);
}

public sealed record PermissionDefinition(string Code, string Description);
