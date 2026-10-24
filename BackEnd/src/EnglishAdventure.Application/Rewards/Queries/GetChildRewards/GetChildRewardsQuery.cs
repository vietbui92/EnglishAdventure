using EnglishAdventure.Application.Rewards.Models;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Queries.GetChildRewards;

/// <summary>One of the Parent's children's points and badges (S5.3). Another family's child is not found.</summary>
public sealed record GetChildRewardsQuery(Guid ChildProfileId) : IRequest<RewardsOverview>;
