namespace EnglishAdventure.Domain.Enums;

/// <summary>What a lesson is in its level (S3.8, PD-011).</summary>
public enum LessonKind
{
    /// <summary>A lesson with its own word bank, learn step and activities, in the level's order.</summary>
    Regular = 1,

    /// <summary>The level's boss challenge: after every lesson, with activities built from the lessons' words.</summary>
    Boss = 2
}
