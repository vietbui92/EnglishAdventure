namespace EnglishAdventure.Application.Common.Exceptions;

/// <summary>
/// Generic sign-in failure. The message is deliberately the same for an unknown email, a wrong password,
/// and a locked-out user, so the response does not reveal which one occurred.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("The email or password is incorrect, or the user is temporarily locked.")
    {
    }
}
