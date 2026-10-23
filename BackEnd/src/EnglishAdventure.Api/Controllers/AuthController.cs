using EnglishAdventure.Api.Authentication;
using EnglishAdventure.Api.Contracts.Authentication;
using EnglishAdventure.Application.Authentication.Commands.Login;
using EnglishAdventure.Application.Authentication.Commands.Logout;
using EnglishAdventure.Application.Authentication.Commands.RefreshSession;
using EnglishAdventure.Application.Authentication.Models;
using EnglishAdventure.Application.Authentication.Queries.GetCurrentUser;
using EnglishAdventure.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthTokensResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(CreateTokensResponse(tokens));
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
        var refreshToken = request?.RefreshToken ?? string.Empty;
        if (RefreshTokenCookie.IsWebClient(Request))
        {
            // A web client without the cookie is simply not signed in.
            refreshToken = RefreshTokenCookie.Read(Request) ?? throw new RefreshTokenException();
        }

        var tokens = await _mediator.Send(new RefreshSessionCommand(refreshToken), cancellationToken);
        return Ok(CreateTokensResponse(tokens));
    }

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
