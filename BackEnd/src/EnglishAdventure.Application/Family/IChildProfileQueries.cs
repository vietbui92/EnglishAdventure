using EnglishAdventure.Application.Family.Models;

namespace EnglishAdventure.Application.Family;

/// <summary>Read-only views of one Parent's child profiles. Every method is limited to that Parent's family.</summary>
public interface IChildProfileQueries
{
    /// <summary>Active profiles first, each group in the order they were created.</summary>
    Task<IReadOnlyList<ChildProfileSummary>> ListAsync(
        Guid parentUserId,
        bool includeArchived,
        CancellationToken cancellationToken);

    Task<ChildProfileSummary?> GetAsync(Guid profileId, Guid parentUserId, CancellationToken cancellationToken);
}
