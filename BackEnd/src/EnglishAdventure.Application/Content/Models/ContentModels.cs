namespace EnglishAdventure.Application.Content.Models;

/// <param name="Status">"Draft", "Published" or "Archived".</param>
/// <param name="Version">Send it back with a change; a stale version answers 409.</param>
/// <param name="BossStatus">The status of the level's boss that is not archived, null when there is none (S3.8).</param>
public sealed record LevelSummary(
    Guid Id,
    string Title,
    string? Description,
    int SortOrder,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? ArchivedAtUtc,
    LessonCounts Lessons,
    string Version,
    string? BossStatus);

/// <summary>The level's regular lessons by status; the boss is counted apart (<see cref="LevelSummary.BossStatus"/>).</summary>
public sealed record LessonCounts(int Draft, int Published, int Archived);

public sealed record LevelDetails(LevelSummary Level, IReadOnlyList<LessonSummary> Lessons);

/// <summary>
/// A lesson's word bank (S3.6) with what a client needs to change it. For a boss (S3.8): the words of the level's
/// lessons that its activities may use, read-only here, with <see cref="MaxWords"/> 0.
/// </summary>
/// <param name="LessonStatus">Words are added, removed and reordered only while "Draft" (PD-012).</param>
/// <param name="LessonVersion">The lesson's version: every word change sends it and changes it.</param>
/// <param name="MaxWords">How many words a lesson may have.</param>
public sealed record LessonWordBank(
    Guid LessonId,
    string LessonStatus,
    string LessonVersion,
    int MaxWords,
    IReadOnlyList<LessonWordSummary> Words);

/// <param name="ImageUrl">Null while the Admin has not added a picture yet; required before publishing.</param>
/// <param name="ShowInLearnStep">Whether the word is a card of the learn step (S3.7).</param>
public sealed record LessonWordSummary(
    Guid Id,
    int SortOrder,
    string English,
    string Meaning,
    string? ImageUrl,
    bool ShowInLearnStep);

/// <summary>A lesson's activities (S3.2) with what a client needs to change them. Admin view: answers included.</summary>
/// <param name="LessonVersion">The lesson's version: every activity change sends it and changes it.</param>
public sealed record LessonActivities(
    Guid LessonId,
    string LessonStatus,
    string LessonVersion,
    int MaxActivities,
    int MaxQuestionsPerQuiz,
    IReadOnlyList<ActivitySummary> Activities);

/// <param name="Type">"Quiz" (S3.2) or "Matching" (S3.3).</param>
/// <param name="Questions">The quiz's questions; empty for a matching.</param>
/// <param name="MatchingMode">For a matching: "WordToPicture" or "WordToMeaning"; null for a quiz.</param>
/// <param name="PairWordIds">For a matching: the words to match, in the Admin's order; empty for a quiz.</param>
public sealed record ActivitySummary(
    Guid Id,
    string Type,
    int SortOrder,
    IReadOnlyList<QuizQuestionSummary> Questions,
    string? MatchingMode,
    IReadOnlyList<Guid> PairWordIds);

/// <param name="Form">"PictureToWord", "WordToPicture" or "FillInTheBlank".</param>
/// <param name="WrongWordIds">The wrong options, in the Admin's order.</param>
/// <param name="Sentence">Only for "FillInTheBlank": the sentence with its gap (<c>___</c>).</param>
public sealed record QuizQuestionSummary(
    Guid Id,
    int SortOrder,
    string Form,
    Guid CorrectWordId,
    IReadOnlyList<Guid> WrongWordIds,
    string? Sentence);

/// <param name="Title">For a boss: the boss's name.</param>
/// <param name="Status">"Draft", "Published" or "Archived".</param>
/// <param name="Version">Send it back with a change; a stale version answers 409.</param>
/// <param name="Kind">"Regular" or "Boss" (S3.8). A boss is not in the lessons' order.</param>
/// <param name="BossImageUrl">The boss's picture; null for a regular lesson or a boss without one yet.</param>
public sealed record LessonSummary(
    Guid Id,
    Guid LevelId,
    string Title,
    int SortOrder,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? ArchivedAtUtc,
    string Version,
    string Kind,
    string? BossImageUrl);
