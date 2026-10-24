namespace EnglishAdventure.Application.Content.Models;

// What a child session reads to play (S3.4). Nothing here tells which option or partner is right: options carry their
// own identities (never word ids) and come shuffled, and the two sides of a matching have different identities.
// Locks and stars come with S4.2.

/// <summary>The published adventure: levels in order with their lessons and boss.</summary>
public sealed record LearnerMap(IReadOnlyList<MapLevel> Levels);

/// <param name="Boss">The level's published boss challenge, after its lessons.</param>
public sealed record MapLevel(Guid Id, string Title, string? Description, IReadOnlyList<MapLesson> Lessons, MapBoss? Boss);

public sealed record MapLesson(Guid Id, string Title);

public sealed record MapBoss(Guid Id, string Name, string? ImageUrl);

/// <summary>One lesson or boss as a child plays it: the learn step's cards, then the activities.</summary>
/// <param name="Kind">"Regular" or "Boss"; a boss has no learn cards.</param>
/// <param name="BossImageUrl">The boss's picture; null for a regular lesson.</param>
public sealed record PlayLesson(
    Guid Id,
    Guid LevelId,
    string Title,
    string Kind,
    string? BossImageUrl,
    IReadOnlyList<LearnCard> LearnCards,
    IReadOnlyList<PlayActivity> Activities);

/// <summary>
/// A word the child meets: first on the discovery walk (its picture moving as <see cref="Movement"/> says, its sound),
/// then as a review card (S3.7, S3.9, PD-013). The word is read aloud by the device's voice.
/// </summary>
/// <param name="Movement">"Still", "Walk", "Hop", "Fly", "Swim", "Crawl" or "Wiggle".</param>
/// <param name="SoundUrl">The sound the word makes (a meow), if any.</param>
public sealed record LearnCard(string English, string Meaning, string? ImageUrl, string Movement, string? SoundUrl);

/// <param name="Type">"Quiz" or "Matching".</param>
/// <param name="Questions">A quiz's questions in order; empty for a matching.</param>
/// <param name="Matching">A matching's two sides; null for a quiz.</param>
public sealed record PlayActivity(Guid Id, string Type, IReadOnlyList<PlayQuestion> Questions, PlayMatching? Matching);

/// <summary>
/// One quiz question. "PictureToWord": <see cref="PromptImageUrl"/>, options with text. "WordToPicture":
/// <see cref="PromptText"/> (the English word), options with pictures. "FillInTheBlank": <see cref="PromptText"/> (the
/// sentence with its <c>___</c>), options with text.
/// </summary>
/// <param name="Options">Shuffled every time.</param>
public sealed record PlayQuestion(
    Guid Id,
    string Form,
    string? PromptText,
    string? PromptImageUrl,
    IReadOnlyList<PlayChoice> Options);

/// <summary>Something to pick: a text (an English word or a meaning) or a picture.</summary>
public sealed record PlayChoice(Guid Id, string? Text, string? ImageUrl);

/// <param name="Mode">"WordToPicture" or "WordToMeaning".</param>
/// <param name="Left">The English words, shuffled.</param>
/// <param name="Right">Their pictures or meanings, shuffled apart and with other identities.</param>
public sealed record PlayMatching(string Mode, IReadOnlyList<PlayChoice> Left, IReadOnlyList<PlayChoice> Right);
