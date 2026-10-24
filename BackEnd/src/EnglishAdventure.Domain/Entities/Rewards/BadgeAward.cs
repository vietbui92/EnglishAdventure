using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Rewards;

/// <summary>
/// A badge a child has won (S5.2, PD-011): at most once per child and kind, and per level for the level badges (a
/// unique index). Never taken back, even when the level changes later (owner 2026-10-24).
/// </summary>
public sealed class BadgeAward : Entity
{
    private BadgeAward()
    {
    }

    public Guid ChildProfileId { get; private set; }

    public BadgeKind Kind { get; private set; }

    /// <summary>The level of a level badge; null for the others.</summary>
    public Guid? LevelId { get; private set; }

    /// <summary>The lesson whose result won it.</summary>
    public Guid LessonId { get; private set; }

    public DateTime AwardedAtUtc { get; private set; }

    public static BadgeAward Win(Guid childProfileId, BadgeKind kind, Guid? levelId, Guid lessonId, DateTime nowUtc)
    {
        if (BadgeDefinition.IsPerLevel(kind) != levelId.HasValue)
        {
            throw new DomainException("Only a level badge belongs to a level.");
        }

        return new BadgeAward
        {
            Id = Guid.NewGuid(),
            ChildProfileId = childProfileId,
            Kind = kind,
            LevelId = levelId,
            LessonId = lessonId,
            AwardedAtUtc = nowUtc,
        };
    }
}
