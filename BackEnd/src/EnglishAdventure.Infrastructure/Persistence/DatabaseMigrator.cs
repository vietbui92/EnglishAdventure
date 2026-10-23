using EnglishAdventure.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence;

internal sealed class DatabaseMigrator : IDatabaseMigrator
{
    private readonly EnglishAdventureDbContext _dbContext;

    public DatabaseMigrator(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken)
    {
        var pendingMigrations = (await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        // MigrateAsync takes a database lock, so concurrent calls do not apply the same migration twice. It also
        // fails when the model has changes that no migration covers: add a migration first.
        if (pendingMigrations.Count > 0)
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }

        return pendingMigrations;
    }
}
