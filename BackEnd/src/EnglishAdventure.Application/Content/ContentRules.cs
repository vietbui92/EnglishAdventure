using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
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

    /// <summary>The lesson when it has the word; another lesson's word is reported as not found.</summary>
    public static async Task<Lesson> GetLessonWithWordAsync(
        ILessonRepository lessons,
        Guid lessonId,
        Guid wordId,
        CancellationToken cancellationToken)
    {
        var lesson = await GetLessonAsync(lessons, lessonId, cancellationToken);
        return lesson.Words.Any(word => word.Id == wordId) ? lesson : throw new NotFoundException("LessonWord", wordId);
    }

    /// <summary>For a boss, the words of its level's lessons, which its activities may use; null for a lesson (S3.8).</summary>
    public static async Task<LevelWords?> LevelWordsForAsync(
        ILessonRepository lessons,
        Lesson lesson,
        CancellationToken cancellationToken) =>
        lesson.IsBoss ? await lessons.GetLevelWordsAsync(lesson.LevelId, cancellationToken) : null;

    /// <summary>The lesson when it has the activity (and the question, when given); otherwise not found.</summary>
    public static async Task<Lesson> GetLessonWithActivityAsync(
        ILessonRepository lessons,
        Guid lessonId,
        Guid activityId,
        Guid? questionId,
        CancellationToken cancellationToken)
    {
        var lesson = await GetLessonAsync(lessons, lessonId, cancellationToken);
        var activity = lesson.Activities.SingleOrDefault(candidate => candidate.Id == activityId)
                       ?? throw new NotFoundException("LessonActivity", activityId);
        if (questionId is { } id && (activity is not QuizActivity quiz || quiz.Questions.All(question => question.Id != id)))
        {
            throw new NotFoundException("QuizQuestion", id);
        }

        return lesson;
    }

    /// <summary>Like reordering levels and lessons: a list that does not match means the items changed meanwhile.</summary>
    public static void EnsureSameItems(IEnumerable<Guid> currentIds, IReadOnlyList<Guid> orderedIds)
    {
        var current = currentIds.ToHashSet();
        if (orderedIds.Count != current.Count || !orderedIds.All(current.Contains))
        {
            throw new ConflictException("The list does not match the current items. Reload it and try again.");
        }
    }

    public static IRuleBuilderOptions<T, string> ValidEnglishWord<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(english => (english?.Trim().Length ?? 0) is > 0 and <= LessonWord.EnglishMaxLength)
            .WithMessage($"The English word must have 1 to {LessonWord.EnglishMaxLength} characters.");

    public static IRuleBuilderOptions<T, string> ValidMeaning<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(meaning => (meaning?.Trim().Length ?? 0) is > 0 and <= LessonWord.MeaningMaxLength)
            .WithMessage($"The meaning must have 1 to {LessonWord.MeaningMaxLength} characters.");

    /// <summary>Null or blank is "Still"; otherwise a <see cref="WordMovement"/> name exactly as listed (S3.9).</summary>
    public static IRuleBuilderOptions<T, string?> ValidOptionalMovement<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(movement => string.IsNullOrWhiteSpace(movement) || WordMovements.TryParse(movement, out _))
            .WithMessage($"The movement must be one of: {string.Join(", ", Enum.GetNames<WordMovement>())}.");

    /// <summary>Blank means no sound; otherwise an https:// address (S3.9, PD-013).</summary>
    public static IRuleBuilderOptions<T, string?> ValidOptionalSoundUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(url => string.IsNullOrWhiteSpace(url) || SoundUrl.IsValid(url))
            .WithMessage($"The sound must be an https:// address of at most {SoundUrl.MaxLength} characters.");

    /// <summary>The discovery fields of a word command, already validated.</summary>
    public static WordDiscovery Discovery(string? movement, string? soundUrl) =>
        new(
            string.IsNullOrWhiteSpace(movement) ? WordMovement.Still : WordMovements.Parse(movement),
            SoundUrl.FromOptional(soundUrl));

    /// <summary>Blank means no picture yet; otherwise an https:// address (PD-012, owner 2026-10-24).</summary>
    public static IRuleBuilderOptions<T, string?> ValidOptionalImageUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(url => string.IsNullOrWhiteSpace(url) || ImageUrl.IsValid(url))
            .WithMessage($"The picture must be an https:// address of at most {ImageUrl.MaxLength} characters.");

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

public static class WordMovements
{
    /// <summary>The movement's name exactly as listed; numbers are not accepted.</summary>
    public static bool TryParse(string? value, out WordMovement movement)
    {
        movement = default;
        return !string.IsNullOrEmpty(value)
               && Enum.GetNames<WordMovement>().Contains(value, StringComparer.Ordinal)
               && Enum.TryParse(value, out movement);
    }

    public static WordMovement Parse(string value) =>
        TryParse(value, out var movement) ? movement : throw new FormatException("Invalid movement.");
}
