using EnglishAdventure.Domain.Entities.Content;

namespace EnglishAdventure.Application.Content;

public interface ILevelRepository
{
    Task<Level?> GetAsync(Guid levelId, CancellationToken cancellationToken);

    /// <summary>The non-archived levels in their order.</summary>
    Task<IReadOnlyList<Level>> ListInOrderAsync(CancellationToken cancellationToken);

    /// <summary>The highest position among the non-archived levels, 0 when there are none.</summary>
    Task<int> MaxSortOrderAsync(CancellationToken cancellationToken);

    void Add(Level level);

    /// <summary>
    /// Saving fails with a conflict unless the level is still at <paramref name="version"/>, the version the client
    /// last read (optimistic concurrency).
    /// </summary>
    void ExpectVersion(Level level, byte[] version);
}
