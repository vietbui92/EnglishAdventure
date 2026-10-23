namespace EnglishAdventure.Application.Common.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Saves every change made through the repositories in one transaction.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
