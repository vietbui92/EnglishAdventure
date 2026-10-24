using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.ValueObjects;
using MediatR;

namespace EnglishAdventure.Application.Content.Commands.UpdateBoss;

public sealed class UpdateBossCommandHandler : IRequestHandler<UpdateBossCommand>
{
    private readonly ILevelRepository _levels;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBossCommandHandler(ILevelRepository levels, ILessonRepository lessons, IUnitOfWork unitOfWork)
    {
        _levels = levels;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateBossCommand request, CancellationToken cancellationToken)
    {
        var boss = await ContentRules.GetLessonAsync(_lessons, request.LessonId, cancellationToken);
        var level = await ContentRules.GetLevelAsync(_levels, boss.LevelId, cancellationToken);
        _lessons.ExpectVersion(boss, ContentVersion.Decode(request.Version));

        boss.UpdateBoss(level, ContentTitle.From(request.Title), ImageUrl.FromOptional(request.ImageUrl));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
