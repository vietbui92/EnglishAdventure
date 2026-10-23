using EnglishAdventure.Api.Authentication;
using EnglishAdventure.Api.Contracts.Authentication;
using EnglishAdventure.Api.RateLimiting;
using EnglishAdventure.Application.Authentication.Commands.ConfirmEmail;
using EnglishAdventure.Application.Authentication.Commands.Login;
using EnglishAdventure.Application.Authentication.Commands.Logout;
using EnglishAdventure.Application.Authentication.Commands.RefreshSession;
using EnglishAdventure.Application.Authentication.Commands.RegisterParent;
using EnglishAdventure.Application.Authentication.Commands.ResendEmailConfirmation;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Authentication.Queries.GetCurrentUser;
using EnglishAdventure.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishAdventure.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Signs in with email and password and returns an access token and a refresh token. With the
    /// <c>X-EnglishAdventure-Client: web</c> header the refresh token is set as an httpOnly cookie instead.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingDependencyInjection.SignInPolicy)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthTokensResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(CreateTokensResponse(tokens));
    }

    /// <summary>
    /// Registers a Parent account (PD-009) and emails a confirmation link; the account cannot sign in before it is
    /// confirmed. Always 202 with no body, also when the email is taken, so the response never reveals who is
    /// registered (the owner of a taken email gets a note instead).
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingDependencyInjection.RegistrationPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new RegisterParentCommand(request.Email, request.Password, request.AcceptTerms, request.Language),
            cancellationToken);
        return Accepted();
    }

    /// <summary>
    /// Confirms the email of a registered Parent with the token from the emailed link. Opening the same link again
    /// answers 204 too (nothing changes); an unknown, expired or replaced token gives 400.
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingDependencyInjection.RegistrationPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ConfirmEmailCommand(request.Token), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Emails a new confirmation link if the account exists and is not confirmed yet. Always 202, whatever the email.
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingDependencyInjection.RegistrationPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendConfirmation(
        ResendEmailConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResendEmailConfirmationCommand(request.Email, request.Language), cancellationToken);
        return Accepted();
    }

    /// <summary>
    /// Exchanges a refresh token for a new token pair. The presented refresh token can only be used once.
    /// Web clients send no body: the refresh token is read from the cookie.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshSessionRequest? request,
        CancellationToken cancellationToken)
    {
        if (RefreshTokenCookie.IsWebClient(Request))
        {
            return await RefreshFromCookieAsync(cancellationToken);
        }

        var tokens = await _mediator.Send(
            new RefreshSessionCommand(request?.RefreshToken ?? string.Empty),
            cancellationToken);
        return Ok(CreateTokensResponse(tokens));
    }

    // The web app calls refresh on every page load, so "not signed in" is an ordinary answer here, not an error: it is
    // returned without an exception. A rejected cookie (revoked, expired, reused) is deleted, which the exception
    // handler could not do because it clears the response headers; otherwise the browser would keep sending it.
    private async Task<ActionResult<AuthTokensResponse>> RefreshFromCookieAsync(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(Request);
        if (refreshToken is null)
        {
            return SessionEnded();
        }

        TokenPair tokens;
        try
        {
            tokens = await _mediator.Send(new RefreshSessionCommand(refreshToken), cancellationToken);
        }
        catch (RefreshTokenException)
        {
            RefreshTokenCookie.Delete(Response);
            return SessionEnded();
        }

        return Ok(CreateTokensResponse(tokens));
    }

    // Same body as GlobalExceptionHandler gives a RefreshTokenException.
    private ObjectResult SessionEnded() =>
        Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Authentication failed.",
            detail: new RefreshTokenException().Message,
            instance: Request.Path);

    /// <summary>
    /// Ends the session that the refresh token belongs to. Idempotent. Access tokens already issued stay valid
    /// until they expire. Web clients send no body: the cookie's session ends and the cookie is deleted.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutRequest? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = request?.RefreshToken ?? string.Empty;
        if (RefreshTokenCookie.IsWebClient(Request))
        {
            refreshToken = RefreshTokenCookie.Read(Request) ?? string.Empty;
            RefreshTokenCookie.Delete(Response);
            if (refreshToken.Length == 0)
            {
                return NoContent();
            }
        }

        await _mediator.Send(new LogoutCommand(refreshToken), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Returns the user behind the presented access token.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(CurrentUserResponse.From(profile));
    }

    private AuthTokensResponse CreateTokensResponse(TokenPair tokens)
    {
        if (!RefreshTokenCookie.IsWebClient(Request))
        {
            return AuthTokensResponse.From(tokens);
        }

        RefreshTokenCookie.Write(Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);
        return AuthTokensResponse.From(tokens, includeRefreshToken: false);
    }
}
