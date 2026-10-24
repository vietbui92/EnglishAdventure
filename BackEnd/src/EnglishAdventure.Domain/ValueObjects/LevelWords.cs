namespace EnglishAdventure.Domain.ValueObjects;

/// <summary>
/// The words a level's boss can use (S3.8, owner 2026-10-24): the words of the level's lessons, which own them. The
/// boss lesson only points to them, so the application loads them and hands them to the boss for its checks.
/// </summary>
public sealed class LevelWords
{
    private readonly IReadOnlyDictionary<Guid, bool> _hasPicture;

    private LevelWords(IReadOnlyDictionary<Guid, bool> hasPicture)
    {
        _hasPicture = hasPicture;
    }

    public static LevelWords From(IEnumerable<(Guid WordId, bool HasPicture)> words) =>
        new(words.ToDictionary(word => word.WordId, word => word.HasPicture));

    public bool Contains(Guid wordId) => _hasPicture.ContainsKey(wordId);

    public bool HasPicture(Guid wordId) => _hasPicture.TryGetValue(wordId, out var hasPicture) && hasPicture;
}
