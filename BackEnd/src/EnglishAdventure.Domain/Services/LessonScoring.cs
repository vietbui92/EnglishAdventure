namespace EnglishAdventure.Domain.Services;

/// <summary>Stars from a score (PD-011, owner 2026-10-24).</summary>
public static class LessonScoring
{
    public const int OneStarPercent = 60;
    public const int TwoStarsPercent = 80;
    public const int ThreeStarsPercent = 100;

    /// <summary>The score in whole percent, rounded down, so 59.9% is not a pass.</summary>
    public static int Percent(int correctItems, int totalItems) =>
        totalItems <= 0 ? 0 : correctItems * 100 / totalItems;

    /// <summary>0 below 60%, ⭐ from 60%, ⭐⭐ from 80%, ⭐⭐⭐ at 100%. One star or more completes a lesson.</summary>
    public static int Stars(int percent) => percent switch
    {
        >= ThreeStarsPercent => 3,
        >= TwoStarsPercent => 2,
        >= OneStarPercent => 1,
        _ => 0,
    };
}
