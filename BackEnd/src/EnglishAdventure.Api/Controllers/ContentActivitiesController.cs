using EnglishAdventure.Api.Contracts.Content;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Content.Commands.AddMatchingActivity;
using EnglishAdventure.Application.Content.Commands.AddQuizActivity;
using EnglishAdventure.Application.Content.Commands.AddQuizQuestion;
using EnglishAdventure.Application.Content.Commands.RemoveActivity;
using EnglishAdventure.Application.Content.Commands.RemoveQuizQuestion;
using EnglishAdventure.Application.Content.Commands.ReorderActivities;
using EnglishAdventure.Application.Content.Commands.ReorderQuizQuestions;
using EnglishAdventure.Application.Content.Commands.UpdateMatchingActivity;
using EnglishAdventure.Application.Content.Commands.UpdateQuizQuestion;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Content.Queries.GetLessonActivities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// A lesson's activities for Admins (S3.2 quizzes, S3.3 matching). Every change sends the lesson's <c>version</c> (409 when stale) and
/// returns all the activities with the new version. Adding, removing and reordering work only in a draft (422
/// <c>lesson-structure-locked</c>); in a published lesson a fill-in sentence can still be corrected. 422 codes:
/// <c>activity-limit</c>, <c>question-limit</c>, <c>question-invalid</c> (a word not in the lesson), <c>content-archived</c>.
/// </summary>
[ApiController]
[Route("api/v1/admin/content/lessons/{lessonId:guid}/activities")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public sealed class ContentActivitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentActivitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The activities in their order, with their questions and answers (Admin view).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Content.ReadDrafts)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status200OK)]
    public async Task<ActionResult<LessonActivities>> List(Guid lessonId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken));
    }

    /// <summary>Adds an empty quiz after the other activities.</summary>
    [HttpPost("quiz")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> AddQuiz(
        Guid lessonId,
        ContentVersionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddQuizActivityCommand(lessonId, request.Version), cancellationToken);
        var activities = await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken);
        return CreatedAtAction(nameof(List), new { lessonId }, activities);
    }

    /// <summary>Adds a matching (S3.3) after the other activities: 2–6 words matched with pictures or meanings.</summary>
    [HttpPost("matching")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> AddMatching(
        Guid lessonId,
        MatchingRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AddMatchingActivityCommand(lessonId, request.Mode, request.WordIds, request.Version),
            cancellationToken);
        var activities = await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken);
        return CreatedAtAction(nameof(List), new { lessonId }, activities);
    }

    /// <summary>Changes a matching's mode and words (drafts only).</summary>
    [HttpPut("{activityId:guid}/matching")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> UpdateMatching(
        Guid lessonId,
        Guid activityId,
        MatchingRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateMatchingActivityCommand(lessonId, activityId, request.Mode, request.WordIds, request.Version),
            cancellationToken);
        return Ok(await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken));
    }

    /// <summary>Removes an activity with its questions.</summary>
    /// <param name="version">The lesson's version (URL-encoded).</param>
    [HttpDelete("{activityId:guid}")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> Remove(
        Guid lessonId,
        Guid activityId,
        [FromQuery] string version,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveActivityCommand(lessonId, activityId, version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken));
    }

    /// <summary>Puts the activities in a new order. 409 when the list does not name exactly the current activities.</summary>
    [HttpPut("order")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> Reorder(
        Guid lessonId,
        ReorderActivitiesRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReorderActivitiesCommand(lessonId, request.ActivityIds, request.Version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken));
    }

    /// <summary>Adds a question at the end of a quiz. 400 for a question that breaks the shape rules.</summary>
    [HttpPost("{activityId:guid}/questions")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> AddQuestion(
        Guid lessonId,
        Guid activityId,
        QuizQuestionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AddQuizQuestionCommand(
                lessonId,
                activityId,
                request.Form,
                request.CorrectWordId,
                request.WrongWordIds,
                request.Sentence,
                request.Version),
            cancellationToken);
        var activities = await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken);
        return CreatedAtAction(nameof(List), new { lessonId }, activities);
    }

    /// <summary>Changes a question; in a published lesson only the fill-in sentence.</summary>
    [HttpPut("{activityId:guid}/questions/{questionId:guid}")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> UpdateQuestion(
        Guid lessonId,
        Guid activityId,
        Guid questionId,
        QuizQuestionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateQuizQuestionCommand(
                lessonId,
                activityId,
                questionId,
                request.Form,
                request.CorrectWordId,
                request.WrongWordIds,
                request.Sentence,
                request.Version),
            cancellationToken);
        return Ok(await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken));
    }

    /// <summary>Removes a question; the questions after it move up.</summary>
    /// <param name="version">The lesson's version (URL-encoded).</param>
    [HttpDelete("{activityId:guid}/questions/{questionId:guid}")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> RemoveQuestion(
        Guid lessonId,
        Guid activityId,
        Guid questionId,
        [FromQuery] string version,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveQuizQuestionCommand(lessonId, activityId, questionId, version), cancellationToken);
        return Ok(await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken));
    }

    /// <summary>Puts a quiz's questions in a new order. 409 when the list does not name exactly its questions.</summary>
    [HttpPut("{activityId:guid}/questions/order")]
    [Authorize(Policy = Permissions.Content.Manage)]
    [ProducesResponseType(typeof(LessonActivities), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LessonActivities>> ReorderQuestions(
        Guid lessonId,
        Guid activityId,
        ReorderQuizQuestionsRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ReorderQuizQuestionsCommand(lessonId, activityId, request.QuestionIds, request.Version),
            cancellationToken);
        return Ok(await _mediator.Send(new GetLessonActivitiesQuery(lessonId), cancellationToken));
    }
}
