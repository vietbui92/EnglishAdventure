namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>The result of one quiz answer or one match (S4.1).</summary>
/// <param name="CorrectChoiceId">For a quiz question: the right option, shown after answering. Null for a match.</param>
/// <param name="Repeated">The item was already done: this is its stored result, nothing was saved.</param>
public sealed record AnswerOutcome(bool Correct, Guid? CorrectChoiceId, bool Repeated);
