namespace EnglishAdventure.Application.Authentication;

/// <summary>
/// JWT claim names shared by token issuance (Infrastructure) and token validation (Api).
/// Inbound claim mapping is disabled, so these are the raw names that appear in the token.
/// </summary>
public static class AuthClaimTypes
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string AccountType = "account_type";
}
