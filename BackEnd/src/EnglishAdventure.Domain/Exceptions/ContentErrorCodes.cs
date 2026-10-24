namespace EnglishAdventure.Domain.Exceptions;

/// <summary>
/// Codes for the content rules a client reacts to, returned as the ProblemDetails "code" extension with the 422 so the
/// web app can explain the problem without parsing English messages.
/// </summary>
public static class ContentErrorCodes
{
    /// <summary>Archived content, or a lesson of an archived level, cannot change (PD-012).</summary>
    public const string Archived = "content-archived";

    /// <summary>Children have started it, so it cannot be unpublished (PD-012).</summary>
    public const string Started = "content-started";

    /// <summary>The lesson is published: words can be corrected, but not added, removed or reordered (PD-012).</summary>
    public const string StructureLocked = "lesson-structure-locked";

    /// <summary>The lesson cannot be published yet: it has no words, or a word has no picture (S3.6).</summary>
    public const string LessonNotReady = "lesson-not-ready";

    public const string WordLimit = "word-limit";

    public const string DuplicateWord = "word-duplicate";

    /// <summary>A quiz question uses the word, so it cannot be removed (S3.2).</summary>
    public const string WordInUse = "word-in-use";

    /// <summary>A lesson has at most 4 activities (owner, 2026-10-24).</summary>
    public const string ActivityLimit = "activity-limit";

    /// <summary>A quiz has at most 10 questions (owner, 2026-10-24).</summary>
    public const string QuestionLimit = "question-limit";

    /// <summary>A quiz question's options or sentence do not follow the rules, or name a word not in the lesson.</summary>
    public const string InvalidQuestion = "question-invalid";

    /// <summary>A matching does not have 2–6 different words of the lesson (S3.3).</summary>
    public const string InvalidMatching = "matching-invalid";

    /// <summary>A boss uses its level's words; it has no word bank or learn step of its own (S3.8).</summary>
    public const string BossHasNoWordBank = "boss-no-word-bank";

    /// <summary>The level already has a boss; archive it to make a new one (S3.8).</summary>
    public const string BossExists = "boss-exists";

    /// <summary>A level is published only with a published boss (S3.8, owner 2026-10-24).</summary>
    public const string LevelNeedsBoss = "level-needs-boss";

    /// <summary>A published level keeps its boss: unpublish the level first (S3.8).</summary>
    public const string BossRequired = "boss-required";
}
