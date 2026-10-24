using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Learning.Queries.GetLessonToPlay;

/// <summary>
/// A lesson or boss for the current child session to play (S3.4): 404 when it is not published, 422
/// <c>lesson-locked</c> when it is not open for the child yet (S4.2).
/// </summary>
public sealed record GetLessonToPlayQuery(Guid LessonId) : IRequest<PlayLesson>;
