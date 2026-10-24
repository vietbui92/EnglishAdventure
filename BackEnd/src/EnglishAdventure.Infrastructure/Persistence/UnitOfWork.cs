using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EnglishAdventure.Infrastructure.Persistence;

/// <summary>
/// Saves the request's changes in one transaction and turns database conflicts into <see cref="ConflictException"/>
/// (409): a concurrent edit (row version) or a duplicate value caught by a unique index.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    // SQL Server: "Cannot insert duplicate key row" (unique index) and "Violation of UNIQUE KEY constraint".
    private static readonly int[] UniqueViolationErrorNumbers = [2601, 2627];

    private readonly EnglishAdventureDbContext _dbContext;

    public UnitOfWork(EnglishAdventureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException("The data was changed by someone else. Reload it and try again.", exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException
                  && UniqueViolationErrorNumbers.Contains(sqlException.Number))
        {
            throw new ConflictException("A record with the same unique value already exists.", exception);
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await work(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
