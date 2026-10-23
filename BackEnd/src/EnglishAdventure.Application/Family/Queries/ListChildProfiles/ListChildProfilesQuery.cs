using EnglishAdventure.Application.Family.Models;
using MediatR;

namespace EnglishAdventure.Application.Family.Queries.ListChildProfiles;

/// <summary>The signed-in Parent's child profiles; archived ones only when asked for.</summary>
public sealed record ListChildProfilesQuery(bool IncludeArchived) : IRequest<IReadOnlyList<ChildProfileSummary>>;
