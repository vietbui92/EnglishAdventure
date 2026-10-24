using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateQuizQuestion;

public sealed class UpdateQuizQuestionCommandHandler : IRequestHandler<UpdateQuizQuestionCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateQuizQuestionCommandHandler(
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

    public async Task Handle(UpdateQuizQuestionCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonWithActivityAsync(
            _lessons,
            request.LessonId,
            request.ActivityId,
            request.QuestionId,
            cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        lesson.UpdateQuizQuestion(
            level,
            request.ActivityId,
            request.QuestionId,
            QuizForms.ToSpec(request),
            _timeProvider.GetUtcNow().UtcDateTime,
            await ContentRules.LevelWordsForAsync(_lessons, lesson, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
