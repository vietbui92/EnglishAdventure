using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Content;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.ReorderQuizQuestions;

public sealed class ReorderQuizQuestionsCommandHandler : IRequestHandler<ReorderQuizQuestionsCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ReorderQuizQuestionsCommandHandler(
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

    public async Task Handle(ReorderQuizQuestionsCommand request, CancellationToken cancellationToken)
    {
        var lesson = await ContentRules.GetLessonWithActivityAsync(
            _lessons,
            request.LessonId,
            request.ActivityId,
            questionId: null,
            cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, lesson.LevelId, cancellationToken);
        _lessons.ExpectVersion(lesson, ContentVersion.Decode(request.Version));
        var questions = (lesson.Activities.Single(activity => activity.Id == request.ActivityId) as QuizActivity)?.Questions ?? [];
        ContentRules.EnsureSameItems(questions.Select(question => question.Id), request.QuestionIds);

        lesson.ReorderQuizQuestions(level, request.ActivityId, request.QuestionIds, _timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
