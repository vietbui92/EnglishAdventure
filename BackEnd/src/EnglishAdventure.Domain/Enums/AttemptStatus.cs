namespace EnglishAdventure.Domain.Enums;

/// <summary>Where a lesson attempt is (S4.1).</summary>
public enum AttemptStatus
{
    /// <summary>Not every question and pair is done yet; the child can come back to it (PD-012).</summary>
    InProgress = 1,

    /// <summary>Every question answered and every pair matched: scored, with its stars.</summary>
    Finished = 2
}
