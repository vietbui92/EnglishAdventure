using EnglishAdventure.Application.Rewards.Models;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Queries.GetMyRewards;

/// <summary>The current child session's points and badges (S5.3), for the badge room.</summary>
public sealed record GetMyRewardsQuery : IRequest<RewardsOverview>;
