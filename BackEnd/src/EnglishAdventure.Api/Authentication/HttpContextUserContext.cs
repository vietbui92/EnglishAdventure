using EnglishAdventure.Application.Authentication;

namespace EnglishAdventure.Api.Authentication;

internal sealed class HttpContextUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var subject = principal.FindFirst(AuthClaimTypes.Subject)?.Value;
            return Guid.TryParse(subject, out var userId) ? userId : null;
        }
    }
}
