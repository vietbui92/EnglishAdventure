using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Application.Authentication;

public interface IAccessTokenIssuer
{
    /// <param name="childProfileId">Set for a child session: the token then names the profile and carries no roles.</param>
    (string AccessToken, DateTime ExpiresAtUtc) Issue(
        Guid userId,
        UserType userType,
        IReadOnlyList<string> roles,
        Guid? childProfileId = null);
}
