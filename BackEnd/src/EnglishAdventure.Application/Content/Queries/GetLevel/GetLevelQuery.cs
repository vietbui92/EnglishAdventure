using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLevel;

/// <summary>One level with all its lessons, drafts and archived ones included.</summary>
public sealed record GetLevelQuery(Guid LevelId) : IRequest<LevelDetails>;
