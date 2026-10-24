using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLessonActivities;

public sealed record GetLessonActivitiesQuery(Guid LessonId) : IRequest<LessonActivities>;
