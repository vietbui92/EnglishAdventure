using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Authorization.Queries.GetPermissionCatalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

[ApiController]
[Route("api/v1/admin/permissions")]
public sealed class AdminPermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminPermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lists the permission catalog: every permission that roles can be given.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDefinition>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PermissionDefinition>>> GetCatalog(CancellationToken cancellationToken)
    {
        var permissions = await _mediator.Send(new GetPermissionCatalogQuery(), cancellationToken);
        return Ok(permissions);
    }
}
