using EnglishAdventure.Application.Family.Commands.CreateChildProfile;
using EnglishAdventure.Application.Family.Commands.UpdateChildProfile;

namespace EnglishAdventure.Application.UnitTests.Family;

public sealed class ChildProfileValidatorTests
{
    [Theory]
    [InlineData("Bin", "fox")]
    [InlineData("  Bông  ", "turtle")]
    public void Create_validator_accepts_a_nickname_and_a_known_avatar(string nickname, string avatarKey)
    {
        Assert.True(new CreateChildProfileCommandValidator().Validate(new CreateChildProfileCommand(nickname, avatarKey)).IsValid);
    }

    [Theory]
    [InlineData("", "fox")]
    [InlineData("   ", "fox")]
    [InlineData("Bin", "dragon")]
    [InlineData("Bin", "")]
    public void Create_validator_rejects_a_blank_nickname_or_an_unknown_avatar(string nickname, string avatarKey)
    {
        Assert.False(new CreateChildProfileCommandValidator().Validate(new CreateChildProfileCommand(nickname, avatarKey)).IsValid);
    }

    [Fact]
    public void Validators_reject_a_nickname_longer_than_50_characters()
    {
        var tooLong = new string('a', 51);

        Assert.False(new CreateChildProfileCommandValidator().Validate(new CreateChildProfileCommand(tooLong, "fox")).IsValid);
        Assert.False(new UpdateChildProfileCommandValidator()
            .Validate(new UpdateChildProfileCommand(Guid.NewGuid(), tooLong, "fox")).IsValid);
    }
}
