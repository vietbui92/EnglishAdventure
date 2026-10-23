using EnglishAdventure.Application.Authentication;
using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.ValueObjects;
using FluentValidation;

namespace EnglishAdventure.Application.Family;

/// <summary>Shared checks of the Family use cases.</summary>
internal static class FamilyRules
{
    /// <summary>The signed-in Parent. The endpoints require authentication, so a missing user is a programming error.</summary>
    public static Guid CurrentParentId(IUserContext userContext) =>
        userContext.UserId ?? throw new UnauthorizedException();

    public static async Task EnsureNicknameFreeAsync(
        IChildProfileRepository profiles,
        Guid parentUserId,
        Nickname nickname,
        Guid? exceptProfileId,
        CancellationToken cancellationToken)
    {
        if (await profiles.NicknameInUseAsync(parentUserId, nickname, exceptProfileId, cancellationToken))
        {
            throw new ConflictException("Another child in this family already uses this nickname.");
        }
    }

    public static IRuleBuilderOptions<T, string> ValidNickname<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(nickname => (nickname?.Trim().Length ?? 0) is > 0 and <= Nickname.MaxLength)
            .WithMessage($"The nickname must have 1 to {Nickname.MaxLength} characters.");

    public static IRuleBuilderOptions<T, string> ValidAvatar<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(ChildProfile.IsKnownAvatar)
            .WithMessage($"The avatar must be one of: {string.Join(", ", ChildProfile.AvatarKeys)}.");
}
