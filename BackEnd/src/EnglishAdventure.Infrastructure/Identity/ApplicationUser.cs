using EnglishAdventure.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace EnglishAdventure.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public AccountType AccountType { get; set; }

    public bool IsActive { get; set; } = true;
}
