using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddLessonWord;

public sealed class AddLessonWordCommandHandler : IRequestHandler<AddLessonWordCommand, Guid>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AddLessonWordCommandHandler(
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

    public async Task<Guid> Handle(AddLessonWordCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        var word = lesson.AddWord(
            level,
            request.English,
            request.Meaning,
            ImageUrl.FromOptional(request.ImageUrl),
            _timeProvider.GetUtcNow().UtcDateTime,
            ContentRules.Discovery(request.Movement, request.SoundUrl));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return word.Id;
    }
}
