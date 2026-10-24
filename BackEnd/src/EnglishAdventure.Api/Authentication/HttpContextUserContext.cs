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

    public Guid? ChildProfileId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true
                || principal.FindFirst(AuthClaimTypes.Session)?.Value != AuthClaimTypes.ChildSession)
            {
                return null;
            }

            var profileId = principal.FindFirst(AuthClaimTypes.ChildProfileId)?.Value;
            return Guid.TryParse(profileId, out var id) ? id : null;
        }
    }
}
