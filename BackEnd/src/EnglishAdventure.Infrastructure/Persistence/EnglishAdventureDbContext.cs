using EnglishAdventure.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core context. It holds the ASP.NET Core Identity schema (users, roles and their
/// relationships) and the refresh-token store. Add domain DbSets and configurations here as modules are introduced.
/// </summary>
public sealed class EnglishAdventureDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public EnglishAdventureDbContext(DbContextOptions<EnglishAdventureDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's base mapping must run first so the configurations below can refine it.
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnglishAdventureDbContext).Assembly);
    }
}
