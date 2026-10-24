using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderLessonWords;

public sealed class ReorderLessonWordsCommandHandler : IRequestHandler<ReorderLessonWordsCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ReorderLessonWordsCommandHandler(
        ILevelRepository levels,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _levels = levels;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ReorderLessonWordsCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        // Like reordering levels and lessons: a list that does not match means the words changed meanwhile.
        var current = lesson.Words.Select(word => word.Id).ToHashSet();
        if (request.WordIds.Count != current.Count || !request.WordIds.All(current.Contains))
        {
            throw new ConflictException("The list does not match the current words. Reload it and try again.");
        }

        lesson.ReorderWords(level, request.WordIds, _timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
