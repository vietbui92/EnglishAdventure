using FluentValidation;

namespace EnglishAdventure.Application.Family.Commands.UpdateChildProfile;

public sealed class UpdateChildProfileCommandValidator : AbstractValidator<UpdateChildProfileCommand>
{
    public UpdateChildProfileCommandValidator()
    {
        RuleFor(command => command.Nickname).ValidNickname();
        RuleFor(command => command.AvatarKey).ValidAvatar();
    }
}
