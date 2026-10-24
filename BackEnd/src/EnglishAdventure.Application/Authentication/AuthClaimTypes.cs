namespace EnglishAdventure.Application.Authentication;

/// <summary>
/// JWT claim names shared by token issuance (Infrastructure) and token validation (Api).
/// Inbound claim mapping is disabled, so these are the raw names that appear in the token.
/// </summary>
public static class AuthClaimTypes
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string UserType = "user_type";

    /// <summary>"child" for a child session (PD-010); absent for a Parent's or an administrator's own session.</summary>
    public const string Session = "session";

    public const string ChildSession = "child";

    /// <summary>The child profile a child session learns as.</summary>
    public const string ChildProfileId = "child_profile_id";
}
