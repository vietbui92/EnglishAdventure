using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.RemoveQuizQuestion;

public sealed class RemoveQuizQuestionCommandHandler : IRequestHandler<RemoveQuizQuestionCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RemoveQuizQuestionCommandHandler(
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

    public async Task Handle(RemoveQuizQuestionCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonWithActivityAsync(
            _lessons,
            request.LessonId,
            request.ActivityId,
            request.QuestionId,
            cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.RemoveQuizQuestion(level, request.ActivityId, request.QuestionId, _timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
