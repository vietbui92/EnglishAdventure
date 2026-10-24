using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLesson;

public sealed record GetLessonQuery(Guid LessonId) : IRequest<LessonSummary>;
