using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Authentication;

public interface IAccessTokenIssuer
{
    (string AccessToken, DateTime ExpiresAtUtc) Issue(
        Guid userId,
        UserType userType,
        IReadOnlyList<string> roles);
}
