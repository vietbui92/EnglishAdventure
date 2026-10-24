namespace EnglishAdventure.Api.Contracts.Content;

/// <param name="Title">1 to 100 characters.</param>
/// <param name="Description">Up to 500 characters; blank means none.</param>
public sealed record CreateLevelRequest(string Title, string? Description);

/// <param name="Version">The <c>version</c> returned with the level.</param>
public sealed record UpdateLevelRequest(string Title, string? Description, string Version);

/// <param name="Title">1 to 100 characters.</param>
public sealed record CreateLessonRequest(string Title);

/// <param name="Version">The <c>version</c> returned with the lesson.</param>
public sealed record UpdateLessonRequest(string Title, string Version);

/// <param name="Title">The boss's name, 1 to 100 characters.</param>
/// <param name="ImageUrl">An https:// picture of the boss, or blank for none yet (required before publishing).</param>
public sealed record CreateBossRequest(string Title, string? ImageUrl);

/// <param name="Version">The <c>version</c> returned with the boss.</param>
public sealed record UpdateBossRequest(string Title, string? ImageUrl, string Version);

/// <param name="Version">The <c>version</c> returned with the level or lesson.</param>
public sealed record ContentVersionRequest(string Version);

/// <param name="LevelIds">Every non-archived level, once, in the new order.</param>
public sealed record ReorderLevelsRequest(IReadOnlyList<Guid> LevelIds);

/// <param name="LessonIds">Every non-archived lesson of the level, once, in the new order.</param>
public sealed record ReorderLessonsRequest(IReadOnlyList<Guid> LessonIds);

/// <param name="English">1 to 50 characters; unique in the lesson (any letter case).</param>
/// <param name="Meaning">The Vietnamese meaning, 1 to 100 characters.</param>
/// <param name="ImageUrl">An https:// address, or blank for none yet (required before publishing).</param>
/// <param name="Version">The lesson's <c>version</c>.</param>
public sealed record LessonWordRequest(string English, string Meaning, string? ImageUrl, string Version);

/// <param name="Show">Whether the word is a card of the learn step.</param>
/// <param name="Version">The lesson's <c>version</c>.</param>
public sealed record LearnCardRequest(bool Show, string Version);

/// <param name="Form">"PictureToWord", "WordToPicture" or "FillInTheBlank".</param>
/// <param name="CorrectWordId">The word that answers the question, from the lesson's word bank.</param>
/// <param name="WrongWordIds">1 to 3 other words of the lesson (2–4 options in all).</param>
/// <param name="Sentence">Only for "FillInTheBlank": 1–200 characters with exactly one gap <c>___</c>.</param>
/// <param name="Version">The lesson's <c>version</c>.</param>
public sealed record QuizQuestionRequest(
    string Form,
    Guid CorrectWordId,
    IReadOnlyList<Guid> WrongWordIds,
    string? Sentence,
    string Version);

/// <param name="Mode">"WordToPicture" or "WordToMeaning".</param>
/// <param name="WordIds">2 to 6 different words of the lesson, in the Admin's order.</param>
/// <param name="Version">The lesson's <c>version</c>.</param>
public sealed record MatchingRequest(string Mode, IReadOnlyList<Guid> WordIds, string Version);

/// <param name="ActivityIds">Every activity of the lesson, once, in the new order.</param>
/// <param name="Version">The lesson's <c>version</c>.</param>
public sealed record ReorderActivitiesRequest(IReadOnlyList<Guid> ActivityIds, string Version);

/// <param name="QuestionIds">Every question of the quiz, once, in the new order.</param>
/// <param name="Version">The lesson's <c>version</c>.</param>
public sealed record ReorderQuizQuestionsRequest(IReadOnlyList<Guid> QuestionIds, string Version);

/// <param name="WordIds">Every word of the lesson, once, in the new order.</param>
/// <param name="Version">The lesson's <c>version</c>.</param>
public sealed record ReorderLessonWordsRequest(IReadOnlyList<Guid> WordIds, string Version);
