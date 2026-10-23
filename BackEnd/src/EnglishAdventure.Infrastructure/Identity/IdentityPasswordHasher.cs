using EnglishAdventure.Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace EnglishAdventure.Infrastructure.Identity;

/// <summary>
/// <see cref="IPasswordHasher"/> backed by Microsoft's <see cref="PasswordHasher{TUser}"/> (PBKDF2 with HMAC-SHA512,
/// random salt). Only the hashing algorithm is reused; no ASP.NET Core Identity tables or stores are involved.
/// </summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    // The algorithm does not use the user argument; one shared placeholder satisfies the generic signature.
    private static readonly object HashSubject = new();

    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashSubject, password);

    public PasswordVerification Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(HashSubject, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SucceededRehashNeeded,
            _ => PasswordVerification.Failed
        };
}
