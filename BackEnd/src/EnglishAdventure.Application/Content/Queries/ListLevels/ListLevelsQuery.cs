using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.ListLevels;

/// <summary>Every level in its order, drafts included; archived ones only when asked for.</summary>
public sealed record ListLevelsQuery(bool IncludeArchived) : IRequest<IReadOnlyList<LevelSummary>>;
