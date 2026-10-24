using EnglishAdventure.Application.Learning.Models;
using MediatR;

namespace EnglishAdventure.Application.Learning.Queries.GetAttempt;

/// <summary>One of the current child's attempts: what is done and, once finished, the result (S4.1).</summary>
public sealed record GetAttemptQuery(Guid AttemptId) : IRequest<AttemptView>;
