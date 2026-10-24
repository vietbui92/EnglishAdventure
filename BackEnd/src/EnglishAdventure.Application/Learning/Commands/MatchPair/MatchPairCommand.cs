using EnglishAdventure.Application.Learning.Models;
using MediatR;

namespace EnglishAdventure.Application.Learning.Commands.MatchPair;

/// <summary>
/// Tries one match of a matching in the child's attempt (S4.1): a left side (English word) with a right side (its
/// picture or meaning). A wrong match can be tried again; a pair counts when right on its first try.
/// </summary>
public sealed record MatchPairCommand(Guid AttemptId, Guid LeftId, Guid RightId) : IRequest<AnswerResult>;
