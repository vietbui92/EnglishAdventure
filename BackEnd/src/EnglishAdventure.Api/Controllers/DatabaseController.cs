using EnglishAdventure.Api.Filters;
using EnglishAdventure.Application.Database.Commands.ApplyMigrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// Development tooling. Every action returns 404 outside the Development environment.
/// </summary>
[ApiController]
[Route("api/v1/database")]
[DevelopmentOnly]
public sealed class DatabaseController : ControllerBase
{
    private readonly IMediator _mediator;

    public DatabaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates the database if needed and applies every pending EF Core migration. Anonymous, because on a fresh
    /// database there are no users to sign in with.
    /// </summary>
    [HttpPost("migrate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApplyMigrationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplyMigrationsResponse>> Migrate(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ApplyMigrationsCommand(), cancellationToken);
        return Ok(response);
    }
}
