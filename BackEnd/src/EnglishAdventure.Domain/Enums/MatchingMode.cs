namespace EnglishAdventure.Domain.Enums;

/// <summary>What children match each English word with in a matching activity (S3.3, owner 2026-10-24).</summary>
public enum MatchingMode
{
    /// <summary>The word's picture. The default.</summary>
    WordToPicture = 1,

    /// <summary>The word's Vietnamese meaning.</summary>
    WordToMeaning = 2
}
