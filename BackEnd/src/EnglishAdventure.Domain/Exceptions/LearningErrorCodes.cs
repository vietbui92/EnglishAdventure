namespace EnglishAdventure.Domain.Exceptions;

/// <summary>Codes for the learning rules a client reacts to, returned as the ProblemDetails "code" with the 422.</summary>
public static class LearningErrorCodes
{
    /// <summary>The question, option, pair or partner is not part of the attempt's lesson (S4.1).</summary>
    public const string InvalidAnswer = "answer-invalid";

    /// <summary>The attempt is finished; start a new one to play again (S4.1).</summary>
    public const string AttemptFinished = "attempt-finished";

    /// <summary>The lesson or boss is not open for the child yet (S4.2): finish the lessons before it first.</summary>
    public const string LessonLocked = "lesson-locked";
}
