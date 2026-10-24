namespace EnglishAdventure.Api.Contracts.Learning;

/// <param name="OptionId">One of the question's options from the play view. No score is ever sent: the server grades.</param>
public sealed record AnswerQuestionRequest(Guid OptionId);

/// <param name="LeftId">A left side (English word) of a matching from the play view.</param>
/// <param name="RightId">A right side (picture or meaning) of the same matching.</param>
public sealed record MatchPairRequest(Guid LeftId, Guid RightId);
