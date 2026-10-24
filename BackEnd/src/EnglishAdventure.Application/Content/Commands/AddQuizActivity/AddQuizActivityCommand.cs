using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddQuizActivity;

/// <summary>Adds an empty quiz after a draft lesson's other activities (S3.2). Returns its Id.</summary>
/// <param name="Version">The lesson's version.</param>
public sealed record AddQuizActivityCommand(Guid LessonId, string Version) : IRequest<Guid>;
