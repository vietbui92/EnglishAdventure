namespace EnglishAdventure.Application.Common.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Saves every change made through the repositories in one transaction.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> in one database transaction, for a use case that must save several times (for
    /// example to move items through temporary positions). Nothing is kept if it throws.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);
}
