using EnglishAdventure.Domain.Users;

namespace EnglishAdventure.Application.Authentication;

public interface IAccessTokenIssuer
{
    (string AccessToken, DateTime ExpiresAtUtc) Issue(
        Guid userId,
        AccountType accountType,
        IReadOnlyList<string> roles);
}
