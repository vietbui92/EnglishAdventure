using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core context for every module (ADR-1). It currently holds the <c>identity</c> schema:
/// users, roles, permissions and refresh tokens. Each entity is mapped by its own configuration class in
/// <c>Persistence/Configurations/&lt;Module&gt;</c>.
/// </summary>
public sealed class EnglishAdventureDbContext : DbContext
{
    public EnglishAdventureDbContext(DbContextOptions<EnglishAdventureDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnglishAdventureDbContext).Assembly);

        // Domain entities create their own Ids. Without this, EF would treat a new child entity with an Id already set
        // (for example a role added to an existing user) as an existing row and try to update it.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(Entity).IsAssignableFrom(entityType.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(Entity.Id))
                .ValueGeneratedNever();
        }
    }
}
