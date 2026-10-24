using EnglishAdventure.Api.Contracts.Learning;
using EnglishAdventure.Application.Authorization;
using EnglishAdventure.Application.Learning.Commands.AnswerQuestion;
using EnglishAdventure.Application.Learning.Commands.MatchPair;
using EnglishAdventure.Application.Learning.Commands.StartLessonAttempt;
using EnglishAdventure.Application.Learning.Models;
using EnglishAdventure.Application.Learning.Queries.GetAttempt;
using EnglishAdventure.Application.Content.Models;
using EnglishAdventure.Application.Learning.Queries.GetAdventure;
using EnglishAdventure.Application.Learning.Queries.GetLessonToPlay;
using EnglishAdventure.Application.Rewards.Models;
using EnglishAdventure.Application.Rewards.Queries.GetMyRewards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.Controllers;

/// <summary>
/// The content a child plays (S3.4), for child sessions only (<c>learning.play</c>, PD-010). Published levels, lessons
/// and bosses only, and never their answers: options carry their own identities and come shuffled. Answering is S4.1;
/// locks, completion and best stars are S4.2.
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

    /// <summary>
    /// The adventure map: published levels in order with their lessons and boss, and where the child is: each level
    /// unlocked or not, each lesson and boss Locked, Available or Completed with its best stars, and the child's level.
    /// </summary>
    [HttpGet("map")]
    [ProducesResponseType(typeof(AdventureMap), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdventureMap>> Map(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetAdventureQuery(), cancellationToken));
    }

    /// <summary>
    /// A lesson or boss to play: learn cards, then activities. 404 unless it and its level are published; 422
    /// <c>lesson-locked</c> when it is not open for the child (an unfinished attempt keeps it open).
    /// </summary>
    /// <summary>The child's points and every badge on the map, won or not (S5.3): the badge room.</summary>
    [HttpGet("rewards")]
    [ProducesResponseType(typeof(RewardsOverview), StatusCodes.Status200OK)]
    public async Task<ActionResult<RewardsOverview>> Rewards(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetMyRewardsQuery(), cancellationToken));
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(PlayLesson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PlayLesson>> Lesson(Guid lessonId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetLessonToPlayQuery(lessonId), cancellationToken));
    }

    /// <summary>
    /// Starts playing the lesson, or resumes the child's unfinished attempt of it (S4.1, PD-012): what is done so far.
    /// 404 unless the lesson and its level are published; a new attempt needs the lesson open (422 <c>lesson-locked</c>).
    /// </summary>
    [HttpPost("lessons/{lessonId:guid}/attempts")]
    [ProducesResponseType(typeof(AttemptView), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AttemptView>> StartAttempt(Guid lessonId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new StartLessonAttemptCommand(lessonId), cancellationToken));
    }

    /// <summary>One of the child's attempts; another child's attempt is not found.</summary>
    [HttpGet("attempts/{attemptId:guid}")]
    [ProducesResponseType(typeof(AttemptView), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptView>> Attempt(Guid attemptId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetAttemptQuery(attemptId), cancellationToken));
    }

    /// <summary>
    /// Answers a quiz question: right or wrong, and the right option. The first answer counts; sending it again returns
    /// the same result. 422 <c>answer-invalid</c> for a question or option not in the lesson.
    /// </summary>
    [HttpPost("attempts/{attemptId:guid}/questions/{questionId:guid}")]
    [ProducesResponseType(typeof(AnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AnswerResult>> AnswerQuestion(
        Guid attemptId,
        Guid questionId,
        AnswerQuestionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new AnswerQuestionCommand(attemptId, questionId, request.OptionId), cancellationToken));
    }

    /// <summary>
    /// Tries one match of a matching: right or wrong (the right partner is not revealed). A wrong match can be tried
    /// again; a pair counts when right on its first try.
    /// </summary>
    [HttpPost("attempts/{attemptId:guid}/matches")]
    [ProducesResponseType(typeof(AnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AnswerResult>> MatchPair(
        Guid attemptId,
        MatchPairRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new MatchPairCommand(attemptId, request.LeftId, request.RightId), cancellationToken));
    }
}
