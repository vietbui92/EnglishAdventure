using EnglishAdventure.Api.Contracts.Admin;
using EnglishAdventure.Application.Users.Commands.CreateUser;
using EnglishAdventure.Application.Users.Commands.SetUserActive;
using EnglishAdventure.Application.Users.Commands.SetUserRoles;
using EnglishAdventure.Application.Users.Models;
using EnglishAdventure.Application.Users.Queries.GetUser;
using EnglishAdventure.Application.Users.Queries.ListUsers;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lists users ordered by email. <paramref name="search"/> matches part of the email.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Users.Read)]
    [ProducesResponseType(typeof(PagedResult<UserSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserSummary>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new ListUsersQuery(page, pageSize, search), cancellationToken));
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = Permissions.Users.Read)]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummary>> Get(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetUserQuery(userId), cancellationToken));
    }

    /// <summary>
    /// Creates an Admin or Parent user. A Parent user gets the Parent role; roles for an Admin user are set
    /// with <c>PUT {id}/roles</c>.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Users.Manage)]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSummary>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await _mediator.Send(
            new CreateUserCommand(request.Email, request.Password, request.UserType),
            cancellationToken);

        var user = await _mediator.Send(new GetUserQuery(userId), cancellationToken);
        return CreatedAtAction(nameof(Get), new { userId }, user);
    }

    /// <summary>
    /// Deactivates the user and ends all of its sessions. You cannot deactivate yourself or the last SuperAdmin.
    /// </summary>
    [HttpPost("{userId:guid}/deactivate")]
    [Authorize(Policy = Permissions.Users.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetUserActiveCommand(userId, IsActive: false), cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/activate")]
    [Authorize(Policy = Permissions.Users.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetUserActiveCommand(userId, IsActive: true), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Replaces the user's roles. You cannot change your own roles or remove the last SuperAdmin.
    /// A Parent user can only have the Parent role; an Admin user cannot have it.
    /// </summary>
    [HttpPut("{userId:guid}/roles")]
    [Authorize(Policy = Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(UserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserSummary>> SetRoles(
        Guid userId,
        SetUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetUserRolesCommand(userId, request.Roles), cancellationToken);
        return Ok(await _mediator.Send(new GetUserQuery(userId), cancellationToken));
    }
}
