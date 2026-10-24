using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.Entities.Content;

/// <summary>
/// A matching game (S3.3): children match 2–6 English words with their pictures or meanings (owner, 2026-10-24).
/// </summary>
public sealed class MatchingActivity : LessonActivity
{
    public const int MinPairs = 2;
    public const int MaxPairs = 6;

    private readonly List<MatchingPair> _pairs = new();

    private MatchingActivity()
    {
    }

    public override ActivityType Type => ActivityType.Matching;

    public MatchingMode Mode { get; private set; }

    /// <summary>The pairs in the Admin's order. Children see both sides shuffled (S3.4).</summary>
    public IReadOnlyList<MatchingPair> Pairs => _pairs.OrderBy(pair => pair.SortOrder).ToList();

    internal static MatchingActivity Create(Guid lessonId, int sortOrder, MatchingMode mode, IReadOnlyList<Guid> wordIds)
    {
        var matching = new MatchingActivity { Id = Guid.NewGuid(), LessonId = lessonId };
        matching.MoveTo(sortOrder);
        matching.Change(mode, wordIds);
        return matching;
    }

    /// <summary>The rules every matching follows; the lesson also checks the words are its own.</summary>
    public static void EnsureValid(MatchingMode mode, IReadOnlyList<Guid> wordIds)
    {
        if (!Enum.IsDefined(mode))
        {
            throw Invalid("Unknown matching mode.");
        }

        if (wordIds.Count < MinPairs || wordIds.Count > MaxPairs)
        {
            throw Invalid($"A matching has {MinPairs} to {MaxPairs} pairs.");
        }

        if (wordIds.Any(id => id == Guid.Empty) || wordIds.Distinct().Count() != wordIds.Count)
        {
            throw Invalid("Each pair is a different word.");
        }
    }

    /// <summary>Replaces the mode and the pairs; pairs that stay keep their identity.</summary>
    internal void Change(MatchingMode mode, IReadOnlyList<Guid> wordIds)
    {
        EnsureValid(mode, wordIds);
        Mode = mode;

        _pairs.RemoveAll(pair => !wordIds.Contains(pair.WordId));
        for (var index = 0; index < wordIds.Count; index++)
        {
            var existing = _pairs.SingleOrDefault(pair => pair.WordId == wordIds[index]);
            if (existing is null)
            {
                _pairs.Add(MatchingPair.Create(Id, wordIds[index], index + 1));
            }
            else
            {
                existing.MoveTo(index + 1);
            }
        }
    }

    /// <summary>A matching is valid from the moment it exists, so it never blocks publishing.</summary>
    internal override void EnsureReadyToPublish()
    {
    }

    internal override bool UsesWord(Guid wordId) => _pairs.Any(pair => pair.WordId == wordId);

    private static DomainException Invalid(string message) => new(message, ContentErrorCodes.InvalidMatching);
}
