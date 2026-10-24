using EnglishAdventure.Application.Common.Persistence;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.AddQuizQuestion;

public sealed class AddQuizQuestionCommandHandler : IRequestHandler<AddQuizQuestionCommand, Guid>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AddQuizQuestionCommandHandler(
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

    public async Task<Guid> Handle(AddQuizQuestionCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonWithActivityAsync(
            _lessons,
            request.LessonId,
            request.ActivityId,
            questionId: null,
            cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));

        var question = lesson.AddQuizQuestion(
            level,
            request.ActivityId,
            QuizForms.ToSpec(request),
            _timeProvider.GetUtcNow().UtcDateTime,
            await ContentRules.LevelWordsForAsync(_lessons, lesson, cancellationToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return question.Id;
    }
}
