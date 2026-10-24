using EnglishAdventure.Api.Contracts.Rewards;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Rewards.Commands.UpdateBadgeDefinition;
using EnglishAdventure.Application.Rewards.Models;
using EnglishAdventure.Application.Rewards.Queries.ListBadgeDefinitions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// The badges' names, descriptions and pictures (S5.2), for Admins with <c>rewards.manage</c>. The four badges and
/// their rules are fixed (PD-011); a level badge's texts may say {level}, replaced by the level's island name.
/// </summary>
[ApiController]
[Route("api/v1/admin/rewards/badges")]
[Authorize(Policy = Permissions.Rewards.Manage)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class AdminRewardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminRewardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BadgeDefinitionView>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BadgeDefinitionView>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ListBadgeDefinitionsQuery(), cancellationToken));
    }

    /// <summary>Changes a badge's texts and picture; children who won it see them too.</summary>
    /// <param name="kind">"FirstLesson", "Perfect", "LevelConqueror" or "LevelSuperstar".</param>
    [HttpPut("{kind}")]
    [ProducesResponseType(typeof(BadgeDefinitionView), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BadgeDefinitionView>> Update(
        string kind,
        BadgeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new UpdateBadgeDefinitionCommand(kind, request.Name, request.Description, request.ImageUrl),
            cancellationToken));
    }
}
