namespace EnglishAdventure.Application.Common.Exceptions;

/// <summary>
/// The request conflicts with the current state, for example a duplicate email or an existing SuperAdmin. Maps to 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
