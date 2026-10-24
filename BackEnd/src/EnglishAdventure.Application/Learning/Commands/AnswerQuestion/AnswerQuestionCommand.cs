using EnglishAdventure.Application.Learning.Models;
using MediatR;

namespace EnglishAdventure.Application.Learning.Commands.AnswerQuestion;

/// <summary>
/// Answers one quiz question of the child's attempt (S4.1). The server grades it; only the first answer counts, and
/// sending it again returns the same result.
/// </summary>
public sealed record AnswerQuestionCommand(Guid AttemptId, Guid QuestionId, Guid OptionId) : IRequest<AnswerResult>;
