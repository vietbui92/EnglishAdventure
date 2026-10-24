using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetPlayLesson;

/// <summary>A published lesson or boss to play, without its answers (S3.4). Anything else is not found.</summary>
public sealed record GetPlayLessonQuery(Guid LessonId) : IRequest<PlayLesson>;
