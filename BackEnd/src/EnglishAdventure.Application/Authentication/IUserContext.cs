namespace EnglishAdventure.Application.Authentication;

public interface IUserContext
{
    /// <summary>The signed-in user; for a child session, the Parent who started it.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// The child profile of a child session, from the access token; null for other sessions. Learning endpoints use
    /// this, never a profile ID sent by the client.
    /// </summary>
    Guid? ChildProfileId { get; }
}
