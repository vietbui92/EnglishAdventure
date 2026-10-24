using EnglishAdventure.Application.Content.Models;
using MediatR;

namespace EnglishAdventure.Application.Content.Queries.GetLessonWords;

public sealed record GetLessonWordsQuery(Guid LessonId) : IRequest<LessonWordBank>;
