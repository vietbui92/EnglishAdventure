using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Rewards;

/// <summary>
/// One line of a child's points ledger (S5.1, PD-004): 10 points for one newly earned star of one lesson (PD-011). Its
/// source, the child, the lesson and the star's number, is unique, so a retry, a replay or two answers at once can
/// never pay the same star twice. Points are never taken back (owner 2026-10-24).
/// </summary>
public sealed class PointsTransaction : Entity
{
    public const int PointsPerStar = 10;

    private PointsTransaction()
    {
    }

    public Guid ChildProfileId { get; private set; }

    public Guid LessonId { get; private set; }

    /// <summary>1 to 3: the star of the lesson this pays for.</summary>
    public int StarNumber { get; private set; }

    public int Points { get; private set; }

    public DateTime AwardedAtUtc { get; private set; }

    public static PointsTransaction ForStar(Guid childProfileId, Guid lessonId, int starNumber, DateTime nowUtc)
    {
        if (childProfileId == Guid.Empty || lessonId == Guid.Empty || starNumber is < 1 or > 3)
        {
            throw new DomainException("Points are paid for star 1, 2 or 3 of a child's lesson.");
        }

        return new PointsTransaction
        {
            Id = Guid.NewGuid(),
            ChildProfileId = childProfileId,
            LessonId = lessonId,
            StarNumber = starNumber,
            Points = PointsPerStar,
            AwardedAtUtc = nowUtc,
        };
    }
}
