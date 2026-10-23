namespace EnglishAdventure.Application.Authentication;

/// <summary>Links to pages of the web app, for emails.</summary>
public interface IAppLinks
{
    string ConfirmEmail(string rawToken);

    string SignIn();
}
