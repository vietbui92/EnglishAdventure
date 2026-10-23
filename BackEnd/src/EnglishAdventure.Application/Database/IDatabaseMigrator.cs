namespace EnglishAdventure.Application.Database;

public interface IDatabaseMigrator
{
    /// <summary>
    /// Creates the database if it does not exist and applies every pending migration. Returns the names of the
    /// migrations that were applied, in order (empty when the schema was already up to date).
    /// </summary>
    Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken);
}
