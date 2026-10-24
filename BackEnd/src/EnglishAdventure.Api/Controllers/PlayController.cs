using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Content.Queries.GetLearnerMap;
using EnglishAdventure.Application.Content.Queries.GetPlayLesson;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// The content a child plays (S3.4), for child sessions only (<c>learning.play</c>, PD-010). Published levels, lessons
/// and bosses only, and never their answers: options carry their own identities and come shuffled. Locks and stars come
/// with S4.2, answering with S4.1.
/// </summary>
[ApiController]
[Route("api/v1/play")]
[Authorize(Policy = Permissions.Learning.Play)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class PlayController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlayController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The adventure map: published levels in order with their lessons and boss.</summary>
    [HttpGet("map")]
    [ProducesResponseType(typeof(LearnerMap), StatusCodes.Status200OK)]
    public async Task<ActionResult<LearnerMap>> Map(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetLearnerMapQuery(), cancellationToken));
    }

    /// <summary>A lesson or boss to play: learn cards, then activities. 404 unless it and its level are published.</summary>
    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(PlayLesson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayLesson>> Lesson(Guid lessonId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetPlayLessonQuery(lessonId), cancellationToken));
    }
}
