using EnglishAdventure.Application.Authentication;
using Microsoft.Extensions.Options;

namespace EnglishAdventure.Infrastructure.Email;

public sealed class WebAppOptions
{
    public const string SectionName = "WebApp";

    /// <summary>The web app's address, for links in emails, e.g. <c>http://localhost:5173</c> in development.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>Builds links to the web app's pages (FrontEnd routes /confirm-email and /login).</summary>
internal sealed class WebAppLinks : IAppLinks
{
    private readonly string _baseUrl;

    public WebAppLinks(IOptions<WebAppOptions> options)
    {
        _baseUrl = options.Value.BaseUrl.TrimEnd('/');
    }

    public string ConfirmEmail(string rawToken) => $"{_baseUrl}/confirm-email?token={Uri.EscapeDataString(rawToken)}";

    public string SignIn() => $"{_baseUrl}/login";
}
