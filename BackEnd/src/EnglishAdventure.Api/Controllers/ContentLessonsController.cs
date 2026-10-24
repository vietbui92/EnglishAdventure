using EnglishAdventure.Api.Contracts.Content;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Content.Commands.AddLessonWord;
using EnglishAdventure.Application.Content.Commands.ArchiveLesson;
using EnglishAdventure.Application.Content.Commands.PublishLesson;
using EnglishAdventure.Application.Content.Commands.RemoveLessonWord;
using EnglishAdventure.Application.Content.Commands.ReorderLessonWords;
using EnglishAdventure.Application.Content.Commands.SetLearnCard;
using EnglishAdventure.Application.Content.Commands.UnpublishLesson;
using EnglishAdventure.Application.Content.Commands.UpdateBoss;
using EnglishAdventure.Application.Content.Commands.UpdateLesson;
using EnglishAdventure.Application.Content.Commands.UpdateLessonWord;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Content.Queries.GetLesson;
using EnglishAdventure.Application.Content.Queries.GetLessonWords;
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

    /// <summary>
    /// 422 with <c>lesson-not-ready</c> until the lesson has a word, every word has a picture and the learn step has a card.
    /// </summary>
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

    /// <summary>Changes the boss's name and picture (S3.8); a published boss keeps a picture.</summary>
    [HttpPut("{lessonId:guid}/boss")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonSummary>> UpdateBoss(
        Guid lessonId,
        UpdateBossRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateBossCommand(lessonId, request.Title, request.ImageUrl, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonQuery(lessonId), cancellationToken));
    }

    /// <summary>
    /// The lesson's word bank (S3.6) with the lesson's status and version. For a boss: the words of the level's lessons
    /// that its activities may use (read-only, <c>maxWords</c> 0).
    /// </summary>
    [HttpGet("{lessonId:guid}/words")]
    [Authorize(Policy = Permissions.Content.ReadDrafts)]
    [ProducesResponseType(typeof(LessonWordBank), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LessonWordBank>> GetWords(Guid lessonId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetLessonWordsQuery(lessonId), cancellationToken));
    }

    /// <summary>
    /// Adds a word at the end of a draft lesson. 422 with a <c>code</c>: <c>lesson-structure-locked</c> (published),
    /// <c>word-limit</c>, <c>word-duplicate</c> or <c>content-archived</c>.
    /// </summary>
    [HttpPost("{lessonId:guid}/words")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonWordBank), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonWordBank>> AddWord(
        Guid lessonId,
        LessonWordRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AddLessonWordCommand(
                lessonId,
                request.English,
                request.Meaning,
                request.ImageUrl,
                request.Movement,
                request.SoundUrl,
                request.Version),
            cancellationToken);
        var words = await _mediator.Send(new GetLessonWordsQuery(lessonId), cancellationToken);
        return CreatedAtAction(nameof(GetWords), new { lessonId }, words);
    }

    /// <summary>
    /// Corrects a word, also while published (PD-012). 422 with <c>lesson-not-ready</c> when a published lesson's word
    /// would lose its picture, or <c>word-duplicate</c>.
    /// </summary>
    [HttpPut("{lessonId:guid}/words/{wordId:guid}")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonWordBank), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonWordBank>> UpdateWord(
        Guid lessonId,
        Guid wordId,
        LessonWordRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateLessonWordCommand(
                lessonId,
                wordId,
                request.English,
                request.Meaning,
                request.ImageUrl,
                request.Movement,
                request.SoundUrl,
                request.Version),
            cancellationToken);
        return Ok(await _mediator.Send(new GetLessonWordsQuery(lessonId), cancellationToken));
    }

    /// <summary>
    /// Shows or hides the word in the learn step (S3.7). Drafts only: 422 <c>lesson-structure-locked</c> when published.
    /// </summary>
    [HttpPut("{lessonId:guid}/words/{wordId:guid}/learn-card")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonWordBank), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonWordBank>> SetLearnCard(
        Guid lessonId,
        Guid wordId,
        LearnCardRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetLearnCardCommand(lessonId, wordId, request.Show, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonWordsQuery(lessonId), cancellationToken));
    }

    /// <summary>Removes a word from a draft lesson. 422 <c>lesson-structure-locked</c> when published.</summary>
    /// <param name="version">The lesson's version (URL-encoded).</param>
    [HttpDelete("{lessonId:guid}/words/{wordId:guid}")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonWordBank), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonWordBank>> RemoveWord(
        Guid lessonId,
        Guid wordId,
        [FromQuery] string version,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveLessonWordCommand(lessonId, wordId, version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonWordsQuery(lessonId), cancellationToken));
    }

    /// <summary>
    /// Puts a draft lesson's words in a new order. 409 when the list does not name exactly the current words; 422
    /// <c>lesson-structure-locked</c> when published.
    /// </summary>
    [HttpPut("{lessonId:guid}/words/order")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonWordBank), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonWordBank>> ReorderWords(
        Guid lessonId,
        ReorderLessonWordsRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReorderLessonWordsCommand(lessonId, request.WordIds, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonWordsQuery(lessonId), cancellationToken));
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
