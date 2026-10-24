using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.ValueObjects;
using FluentValidation;

namespace EnglishAdventure.Application.Content;

/// <summary>Shared checks of the Content use cases.</summary>
internal static class ContentRules
{
    public static async Task<Level> GetLevelAsync(
        ILevelRepository levels,
        Guid levelId,
        CancellationToken cancellationToken) =>
        await levels.GetAsync(levelId, cancellationToken) ?? throw new NotFoundException("Level", levelId);

    public static async Task<Lesson> GetLessonAsync(
        ILessonRepository lessons,
        Guid lessonId,
        CancellationToken cancellationToken) =>
        await lessons.GetAsync(lessonId, cancellationToken) ?? throw new NotFoundException("Lesson", lessonId);

    /// <summary>
    /// Gives <paramref name="items"/> the positions of <paramref name="orderedIds"/>, which must name exactly the
    /// current items: otherwise someone added or archived one meanwhile (409).
    /// </summary>
    /// <remarks>
    /// Positions are unique (a database index), so two items cannot simply swap in one save. Every item first moves past
    /// the current highest position, then to its new one, in one transaction.
    /// </remarks>
    public static async Task ReorderAsync<T>(
        IReadOnlyList<T> items,
        IReadOnlyList<Guid> orderedIds,
        Action<T, int> moveTo,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
        where T : OrderedContent
    {
        var byId = items.ToDictionary(item => item.Id);
        if (orderedIds.Count != byId.Count || !orderedIds.All(byId.ContainsKey))
        {
            throw new ConflictException("The list does not match the current items. Reload it and try again.");
        }

        var offset = items.Count == 0 ? 0 : items.Max(item => item.SortOrder);
        await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                for (var index = 0; index < orderedIds.Count; index++)
                {
                    moveTo(byId[orderedIds[index]], offset + index + 1);
                }

                await unitOfWork.SaveChangesAsync(token);

                for (var index = 0; index < orderedIds.Count; index++)
                {
                    moveTo(byId[orderedIds[index]], index + 1);
                }

                await unitOfWork.SaveChangesAsync(token);
            },
            cancellationToken);
    }

    public static IRuleBuilderOptions<T, string> ValidTitle<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(title => (title?.Trim().Length ?? 0) is > 0 and <= ContentTitle.MaxLength)
            .WithMessage($"The title must have 1 to {ContentTitle.MaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(description => (description?.Trim().Length ?? 0) <= Level.DescriptionMaxLength)
            .WithMessage($"The description must have at most {Level.DescriptionMaxLength} characters.");

    public static IRuleBuilderOptions<T, string> ValidVersion<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(ContentVersion.IsValid)
            .WithMessage("The version must be the one returned with the item.");

    public static IRuleBuilderOptions<T, IReadOnlyList<Guid>> ValidOrder<T>(
        this IRuleBuilder<T, IReadOnlyList<Guid>> rule) =>
        rule
            .Must(ids => ids is not null && ids.All(id => id != Guid.Empty) && ids.Distinct().Count() == ids.Count)
            .WithMessage("The order must list each item once.");
}
