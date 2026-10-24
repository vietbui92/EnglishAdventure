namespace EnglishAdventure.Domain.Enums;

/// <summary>
/// How a word's picture moves when a child meets it on the discovery walk (S3.9, PD-013): the app animates the picture
/// this way, so no animated media is needed.
/// </summary>
public enum WordMovement
{
    /// <summary>Stays in place (a thing, a colour). The default.</summary>
    Still = 1,

    /// <summary>Walks across, like a cat or a dog.</summary>
    Walk = 2,

    /// <summary>Hops, like a rabbit or a frog.</summary>
    Hop = 3,

    /// <summary>Flies in loops, like a bird or a butterfly.</summary>
    Fly = 4,

    /// <summary>Swims in waves, like a fish.</summary>
    Swim = 5,

    /// <summary>Crawls slowly, like a snail or a snake.</summary>
    Crawl = 6,

    /// <summary>Wiggles in place, like a jelly or a flower in the wind.</summary>
    Wiggle = 7
}
