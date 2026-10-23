using FluentValidation;

namespace EnglishAdventure.Application.Family.Commands.CreateChildProfile;

public sealed class CreateChildProfileCommandValidator : AbstractValidator<CreateChildProfileCommand>
{
    public CreateChildProfileCommandValidator()
    {
        RuleFor(command => command.Nickname).ValidNickname();
        RuleFor(command => command.AvatarKey).ValidAvatar();
    }
}
