namespace EnglishAdventure.Application.Authentication;

/// <summary>
/// Hashes and verifies passwords. Raw passwords are never stored; only the returned hash is.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string passwordHash, string password);
}

public enum PasswordVerification
{
    Failed = 0,
    Succeeded = 1,

    /// <summary>The password is correct but the hash uses outdated settings and should be replaced.</summary>
    SucceededRehashNeeded = 2
}
