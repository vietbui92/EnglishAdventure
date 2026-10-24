using EnglishAdventure.Domain.Enums;

namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>
/// How a child meets a word on the discovery walk before the review cards (S3.9, PD-013): how its picture moves and,
/// optionally, the sound it makes. Neither is required to publish.
/// </summary>
public sealed record WordDiscovery(WordMovement Movement, SoundUrl? Sound)
{
    /// <summary>Still, without a sound.</summary>
    public static readonly WordDiscovery Default = new(WordMovement.Still, Sound: null);
}
