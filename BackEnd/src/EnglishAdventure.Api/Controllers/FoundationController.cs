using EnglishAdventure.Application.Foundation.Queries.GetFoundationStatus;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

[ApiController]
[Route("api/v1/foundation")]
public sealed class FoundationController : ControllerBase
{
    private readonly IMediator _mediator;

    public FoundationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Foundation example endpoint — verifies MediatR wiring, not product functionality.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GetFoundationStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetFoundationStatusResponse>> GetStatus(
        [FromQuery] string? echo,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetFoundationStatusQuery(echo), cancellationToken);
        return Ok(response);
    }
}
