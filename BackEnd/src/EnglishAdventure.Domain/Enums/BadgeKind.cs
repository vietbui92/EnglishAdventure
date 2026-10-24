namespace EnglishAdventure.Domain.Enums;

/// <summary>The four badges of PD-011. Each is awarded at most once per child (per level for the level badges).</summary>
public enum BadgeKind
{
    /// <summary>The first completed lesson.</summary>
    FirstLesson = 1,

    /// <summary>The first lesson with three stars.</summary>
    Perfect = 2,

    /// <summary>One per level: its boss is defeated.</summary>
    LevelConqueror = 3,

    /// <summary>One per level: three stars on every lesson of the level, including the boss (PD-012).</summary>
    LevelSuperstar = 4
}
