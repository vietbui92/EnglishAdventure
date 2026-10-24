using EnglishAdventure.Domain.Common;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Learning;

/// <summary>
/// A child has completed a lesson or boss: the first finished attempt with at least one star (S4.2, PD-011). It keeps
/// the best result; replaying can only raise it, never lower it. One per child and lesson.
/// </summary>
public sealed class LessonCompletion : Entity
{
    private LessonCompletion()
    {
    }

    public Guid ChildProfileId { get; private set; }

    public Guid LessonId { get; private set; }

    /// <summary>1 to 3.</summary>
    public int BestStars { get; private set; }

    public int BestScorePercent { get; private set; }

    public DateTime CompletedAtUtc { get; private set; }

    /// <summary>When the best result was reached.</summary>
    public DateTime BestAtUtc { get; private set; }

    /// <summary>Whether a finished attempt completes its lesson: one star or more.</summary>
    public static bool Completes(LessonAttempt attempt) => attempt.IsFinished && attempt.Stars >= 1;

    public static LessonCompletion From(LessonAttempt attempt)
    {
        if (!Completes(attempt))
        {
            throw new DomainException("Only a finished attempt with a star completes a lesson.");
        }

        return new LessonCompletion
        {
            Id = Guid.NewGuid(),
            ChildProfileId = attempt.ChildProfileId,
            LessonId = attempt.LessonId,
            BestStars = attempt.Stars!.Value,
            BestScorePercent = attempt.ScorePercent!.Value,
            CompletedAtUtc = attempt.FinishedAtUtc!.Value,
            BestAtUtc = attempt.FinishedAtUtc!.Value,
        };
    }

    /// <summary>
    /// Keeps a better result of a later attempt: more stars, or the same stars with a higher score. Returns whether it
    /// was better; a worse replay changes nothing.
    /// </summary>
    public bool Improve(LessonAttempt attempt)
    {
        if (!attempt.IsFinished || attempt.ChildProfileId != ChildProfileId || attempt.LessonId != LessonId)
        {
            throw new DomainException("Only a finished attempt of the same child and lesson can improve it.");
        }

        var stars = attempt.Stars!.Value;
        var percent = attempt.ScorePercent!.Value;
        if (stars < BestStars || (stars == BestStars && percent <= BestScorePercent))
        {
            return false;
        }

        BestStars = stars;
        BestScorePercent = percent;
        BestAtUtc = attempt.FinishedAtUtc!.Value;
        return true;
    }
}
