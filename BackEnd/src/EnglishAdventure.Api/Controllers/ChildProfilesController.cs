using EnglishAdventure.Api.Contracts.Family;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Family.Commands.ArchiveChildProfile;
using EnglishAdventure.Application.Family.Commands.CreateChildProfile;
using EnglishAdventure.Application.Family.Commands.RestoreChildProfile;
using EnglishAdventure.Application.Family.Commands.UpdateChildProfile;
using EnglishAdventure.Application.Family.Models;
using EnglishAdventure.Application.Family.Queries.GetChildProfile;
using EnglishAdventure.Application.Family.Queries.ListChildProfiles;
using EnglishAdventure.Application.Rewards.Models;
using EnglishAdventure.Application.Rewards.Queries.GetChildRewards;
using EnglishAdventure.Domain.Entities.Family;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// The signed-in Parent's child profiles (S2.1, PD-010). Every action works only on the caller's own family: another
/// family's profile answers 404, exactly like a missing one.
/// </summary>
[ApiController]
[Route("api/v1/family/children")]
[Authorize(Policy = Permissions.Family.ManageChildren)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ChildProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChildProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The family's profiles, active first. Archived profiles only with <paramref name="includeArchived"/>.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ChildProfileSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ChildProfileSummary>>> List(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new ListChildProfilesQuery(includeArchived), cancellationToken));
    }

    /// <summary>The avatars a profile can use, and how many active profiles a Parent may have.</summary>
    /// <summary>One of the Parent's children's points and badges (S5.3); another family's child is not found.</summary>
    [HttpGet("{profileId:guid}/rewards")]
    [Authorize(Policy = Permissions.Family.ReadProgress)]
    [ProducesResponseType(typeof(RewardsOverview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardsOverview>> Rewards(Guid profileId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetChildRewardsQuery(profileId), cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType(typeof(ChildProfileOptionsResponse), StatusCodes.Status200OK)]
    public ActionResult<ChildProfileOptionsResponse> Options() =>
        Ok(new ChildProfileOptionsResponse(ChildProfile.AvatarKeys, ChildProfile.MaxActivePerParent));

    [HttpGet("{profileId:guid}")]
    [ProducesResponseType(typeof(ChildProfileSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildProfileSummary>> Get(Guid profileId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetChildProfileQuery(profileId), cancellationToken));
    }

    /// <summary>
    /// Adds a profile. 409 when another active profile of the family has the nickname; 422 when the family already has
    /// the maximum number of active profiles.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChildProfileSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChildProfileSummary>> Create(
        ChildProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = await _mediator.Send(
            new CreateChildProfileCommand(request.Nickname, request.AvatarKey),
            cancellationToken);
        var profile = await _mediator.Send(new GetChildProfileQuery(profileId), cancellationToken);
        return CreatedAtAction(nameof(Get), new { profileId }, profile);
    }

    /// <summary>Changes the nickname and avatar of an active profile. 422 for an archived one.</summary>
    [HttpPut("{profileId:guid}")]
    [ProducesResponseType(typeof(ChildProfileSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChildProfileSummary>> Update(
        Guid profileId,
        ChildProfileRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateChildProfileCommand(profileId, request.Nickname, request.AvatarKey),
            cancellationToken);
        return Ok(await _mediator.Send(new GetChildProfileQuery(profileId), cancellationToken));
    }

    /// <summary>Hides the profile and stops its learning; progress is kept. Archiving twice changes nothing.</summary>
    [HttpPost("{profileId:guid}/archive")]
    [ProducesResponseType(typeof(ChildProfileSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildProfileSummary>> Archive(Guid profileId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveChildProfileCommand(profileId), cancellationToken);
        return Ok(await _mediator.Send(new GetChildProfileQuery(profileId), cancellationToken));
    }

    /// <summary>
    /// Brings an archived profile back with its progress. 422 when the family already has the maximum number of active
    /// profiles; 409 when an active profile now uses the same nickname.
    /// </summary>
    [HttpPost("{profileId:guid}/restore")]
    [ProducesResponseType(typeof(ChildProfileSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChildProfileSummary>> Restore(Guid profileId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RestoreChildProfileCommand(profileId), cancellationToken);
        return Ok(await _mediator.Send(new GetChildProfileQuery(profileId), cancellationToken));
    }
}
