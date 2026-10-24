namespace EnglishAdventure.Domain.Enums;

/// <summary>Where a child is with a lesson or boss on the map (S4.2, PD-002). Computed, never stored.</summary>
public enum LessonState
{
    /// <summary>Not open yet: the lesson before it (or, for a boss, a lesson of its level) has no star.</summary>
    Locked = 1,

    /// <summary>Open, not completed yet.</summary>
    Available = 2,

    /// <summary>Completed (at least one star); always open again.</summary>
    Completed = 3
}
