using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLearnerMap;

/// <summary>The adventure map for a child session (S3.4). Locks and stars come with S4.2.</summary>
public sealed record GetLearnerMapQuery : IRequest<LearnerMap>;
