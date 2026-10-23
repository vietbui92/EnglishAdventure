using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Application.Users;

/// <summary>
/// Which roles a user type may hold. A family user never becomes an administrator, and an administrator user
/// is not a family user.
/// </summary>
public static class UserRoleRules
{
    public static void EnsureAllowed(UserType userType, IEnumerable<string> roleNames)
    {
        foreach (var roleName in roleNames)
        {
            var isParentRole = roleName == SystemRoles.Parent;

            if (userType == UserType.Parent && !isParentRole)
            {
                throw new DomainException($"A Parent user can only have the {SystemRoles.Parent} role, not '{roleName}'.");
            }

            if (userType == UserType.Admin && isParentRole)
            {
                throw new DomainException($"An Admin user cannot have the {SystemRoles.Parent} role.");
            }
        }
    }
}
