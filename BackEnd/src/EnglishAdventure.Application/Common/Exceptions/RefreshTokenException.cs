namespace EnglishAdventure.Application.Common.Exceptions;

public sealed class RefreshTokenException : Exception
{
    public RefreshTokenException()
        : base("The refresh token is invalid or expired.")
    {
    }
}
