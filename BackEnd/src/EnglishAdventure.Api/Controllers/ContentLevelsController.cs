using EnglishAdventure.Api.Contracts.Content;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Content.Commands.ArchiveLevel;
using EnglishAdventure.Application.Content.Commands.CreateBoss;
using EnglishAdventure.Application.Content.Commands.CreateLesson;
using EnglishAdventure.Application.Content.Commands.CreateLevel;
using EnglishAdventure.Application.Content.Commands.PublishLevel;
using EnglishAdventure.Application.Content.Commands.ReorderLessons;
using EnglishAdventure.Application.Content.Commands.ReorderLevels;
using EnglishAdventure.Application.Content.Commands.UnpublishLevel;
using EnglishAdventure.Application.Content.Commands.UpdateLevel;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Content.Queries.GetLesson;
using EnglishAdventure.Application.Content.Queries.GetLevel;
using EnglishAdventure.Application.Content.Queries.ListLevels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// Levels of the adventure and the order of their lessons, for Admins (S3.1, PD-012). Changes send the item's
/// <c>version</c>; a stale one answers 409. Archived levels are final and answer 422 to any change.
/// </summary>
[ApiController]
[Route("api/v1/admin/content/levels")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ContentLevelsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentLevelsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The levels in their order, drafts included; archived ones only with <paramref name="includeArchived"/>.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Content.ReadDrafts)]
    [ProducesResponseType(typeof(IReadOnlyList<LevelSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LevelSummary>>> List(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new ListLevelsQuery(includeArchived), cancellationToken));
    }

    /// <summary>The level with all its lessons.</summary>
    [HttpGet("{levelId:guid}")]
    [Authorize(Policy = Permissions.Content.ReadDrafts)]
    [ProducesResponseType(typeof(LevelDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LevelDetails>> Get(Guid levelId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetLevelQuery(levelId), cancellationToken));
    }

    /// <summary>Adds a draft level at the end.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LevelDetails), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LevelDetails>> Create(CreateLevelRequest request, CancellationToken cancellationToken)
    {
        var levelId = await _mediator.Send(new CreateLevelCommand(request.Title, request.Description), cancellationToken);
        var level = await _mediator.Send(new GetLevelQuery(levelId), cancellationToken);
        return CreatedAtAction(nameof(Get), new { levelId }, level);
    }

    /// <summary>Changes the title and description, also while published (fixing texts, PD-012).</summary>
    [HttpPut("{levelId:guid}")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LevelDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LevelDetails>> Update(
        Guid levelId,
        UpdateLevelRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateLevelCommand(levelId, request.Title, request.Description, request.Version),
            cancellationToken);
        return Ok(await _mediator.Send(new GetLevelQuery(levelId), cancellationToken));
    }

    /// <summary>Puts the non-archived levels in a new order. 409 when the list does not name exactly those levels.</summary>
    [HttpPut("order")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(IReadOnlyList<LevelSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<LevelSummary>>> Reorder(
        ReorderLevelsRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReorderLevelsCommand(request.LevelIds), cancellationToken);
        return Ok(await _mediator.Send(new ListLevelsQuery(IncludeArchived: false), cancellationToken));
    }

    /// <summary>422 <c>level-needs-boss</c> until the level's boss is published (S3.8).</summary>
    [HttpPost("{levelId:guid}/publish")]
    [Authorize(Policy = Permissions.Content.Publish)]
    [ProducesResponseType(typeof(LevelDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LevelDetails>> Publish(
        Guid levelId,
        ContentVersionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new PublishLevelCommand(levelId, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLevelQuery(levelId), cancellationToken));
    }

    /// <summary>Back to draft. 422 once children have started any of its lessons (PD-012).</summary>
    [HttpPost("{levelId:guid}/unpublish")]
    [Authorize(Policy = Permissions.Content.Publish)]
    [ProducesResponseType(typeof(LevelDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LevelDetails>> Unpublish(
        Guid levelId,
        ContentVersionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnpublishLevelCommand(levelId, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLevelQuery(levelId), cancellationToken));
    }

    /// <summary>Retires the level for good; its lessons are frozen. Archiving twice changes nothing.</summary>
    [HttpPost("{levelId:guid}/archive")]
    [Authorize(Policy = Permissions.Content.Publish)]
    [ProducesResponseType(typeof(LevelDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LevelDetails>> Archive(
        Guid levelId,
        ContentVersionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveLevelCommand(levelId, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLevelQuery(levelId), cancellationToken));
    }

    /// <summary>Adds a draft lesson at the end of the level. 422 for an archived level.</summary>
    [HttpPost("{levelId:guid}/lessons")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonSummary>> CreateLesson(
        Guid levelId,
        CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var lessonId = await _mediator.Send(new CreateLessonCommand(levelId, request.Title), cancellationToken);
        var lesson = await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken);
        return CreatedAtAction(
            nameof(ContentLessonsController.Get),
            "ContentLessons",
            new { lessonId },
            lesson);
    }

    /// <summary>
    /// Adds the level's boss challenge as a draft (S3.8): a name and a picture. 422 <c>boss-exists</c> when the level
    /// already has one that is not archived.
    /// </summary>
    [HttpPost("{levelId:guid}/boss")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonSummary>> CreateBoss(
        Guid levelId,
        CreateBossRequest request,
        CancellationToken cancellationToken)
    {
        var lessonId = await _mediator.Send(new CreateBossCommand(levelId, request.Title, request.ImageUrl), cancellationToken);
        var boss = await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken);
        return CreatedAtAction(nameof(ContentLessonsController.Get), "ContentLessons", new { lessonId }, boss);
    }

    /// <summary>
    /// Puts the level's non-archived lessons in a new order (the boss is not in it). 409 when the list does not name
    /// exactly those lessons.
    /// </summary>
    [HttpPut("{levelId:guid}/lessons/order")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LevelDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LevelDetails>> ReorderLessons(
        Guid levelId,
        ReorderLessonsRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReorderLessonsCommand(levelId, request.LessonIds), cancellationToken);
        return Ok(await _mediator.Send(new GetLevelQuery(levelId), cancellationToken));
    }
}
