namespace EnglishAdventure.Api.Authentication;

/// <summary>
/// Refresh-token transport for the browser app (B-F1). A web client opts in with the <see cref="ClientHeaderName"/>
/// header: the API then keeps the refresh token in an httpOnly cookie and leaves it out of the response body, so page
/// scripts never see it. Other clients (Scalar, .http files, tests) keep sending and receiving it in the JSON body.
/// The header is also the CSRF guard: a cross-site page cannot send a custom header without a CORS preflight, which
/// the API does not allow, so the cookie is read only when the header is present.
/// </summary>
public static class RefreshTokenCookie
{
    public const string ClientHeaderName = "X-EnglishAdventure-Client";
    public const string WebClientValue = "web";
    public const string CookieName = "ea_refresh";
    public const string CookiePath = "/api/v1/auth";

    public static bool IsWebClient(HttpRequest request) =>
        string.Equals(request.Headers[ClientHeaderName], WebClientValue, StringComparison.Ordinal);

    public static string? Read(HttpRequest request) => request.Cookies[CookieName];

    public static void Write(HttpResponse response, string refreshToken, DateTime expiresAtUtc) =>
        response.Cookies.Append(CookieName, refreshToken, CreateOptions(expiresAtUtc));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(CookieName, CreateOptions(expiresAtUtc: null));

    private static CookieOptions CreateOptions(DateTime? expiresAtUtc) => new()
    {
        HttpOnly = true,
        // Browsers treat http://localhost as secure, so this also works with the Vite dev proxy.
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        IsEssential = true,
        Expires = expiresAtUtc is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc.Value, DateTimeKind.Utc))
    };
}
