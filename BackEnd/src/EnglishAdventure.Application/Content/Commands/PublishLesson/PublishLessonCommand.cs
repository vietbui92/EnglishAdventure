using MediatR;

namespace EnglishAdventure.Application.Content.Commands.PublishLesson;

/// <summary>
/// Publishes a draft lesson of a level that is not archived. Publishing twice changes nothing. The checks that the
/// lesson is ready (words, activities) come with S3.6, S3.2 and S3.3.
/// </summary>
public sealed record PublishLessonCommand(Guid LessonId, string Version) : IRequest;
