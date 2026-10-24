using EnglishAdventure.Application.Rewards.Models;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Queries.ListBadgeDefinitions;

/// <summary>The four badges' texts and pictures, for the Admin (S5.2).</summary>
public sealed record ListBadgeDefinitionsQuery : IRequest<IReadOnlyList<BadgeDefinitionView>>;
