using EnglishAdventure.Api.Contracts.Content;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Content.Commands.ArchiveLesson;
using EnglishAdventure.Application.Content.Commands.PublishLesson;
using EnglishAdventure.Application.Content.Commands.UnpublishLesson;
using EnglishAdventure.Application.Content.Commands.UpdateLesson;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Content.Queries.GetLesson;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// One lesson, for Admins (S3.1, PD-012). Lessons are created and ordered under their level
/// (<c>api/v1/admin/content/levels/{levelId}/lessons</c>). Changes send the lesson's <c>version</c>; a stale one answers
/// 409. Archived lessons, and the lessons of an archived level, answer 422 to any change.
/// </summary>
[ApiController]
[Route("api/v1/admin/content/lessons")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ContentLessonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentLessonsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{lessonId:guid}")]
    [Authorize(Policy = Permissions.Content.ReadDrafts)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LessonSummary>> Get(Guid lessonId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken));
    }

    /// <summary>Changes the title, also while published (fixing texts, PD-012).</summary>
    [HttpPut("{lessonId:guid}")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonSummary>> Update(
        Guid lessonId,
        UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateLessonCommand(lessonId, request.Title, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken));
    }

    [HttpPost("{lessonId:guid}/publish")]
    [Authorize(Policy = Permissions.Content.Publish)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonSummary>> Publish(
        Guid lessonId,
        ContentVersionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new PublishLessonCommand(lessonId, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken));
    }

    /// <summary>Back to draft. 422 once a child has started the lesson (PD-012).</summary>
    [HttpPost("{lessonId:guid}/unpublish")]
    [Authorize(Policy = Permissions.Content.Publish)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonSummary>> Unpublish(
        Guid lessonId,
        ContentVersionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnpublishLessonCommand(lessonId, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken));
    }

    /// <summary>Retires the lesson for good; children's results are kept. Archiving twice changes nothing.</summary>
    [HttpPost("{lessonId:guid}/archive")]
    [Authorize(Policy = Permissions.Content.Publish)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LessonSummary>> Archive(
        Guid lessonId,
        ContentVersionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveLessonCommand(lessonId, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken));
    }
}
