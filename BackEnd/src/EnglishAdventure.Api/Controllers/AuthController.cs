using EnglishAdventure.Api.Contracts.Authentication;
using EnglishAdventure.Application.Authentication.Commands.Login;
using EnglishAdventure.Application.Authentication.Commands.Logout;
using EnglishAdventure.Application.Authentication.Commands.RefreshSession;
using EnglishAdventure.Application.Authentication.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// Signs in with email and password and returns an access token and a refresh token.
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
        return Ok(AuthTokensResponse.From(tokens));
    }

    /// <summary>
    /// Exchanges a refresh token for a new token pair. The presented refresh token can only be used once.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _mediator.Send(new RefreshSessionCommand(request.RefreshToken), cancellationToken);
        return Ok(AuthTokensResponse.From(tokens));
    }

    /// <summary>
    /// Ends the session that the refresh token belongs to. Idempotent. Access tokens already issued stay valid
    /// until they expire.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
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
}
