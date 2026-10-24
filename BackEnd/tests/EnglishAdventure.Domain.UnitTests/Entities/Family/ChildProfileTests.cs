using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Family;

public sealed class ChildProfileTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ParentId = Guid.NewGuid();

    private static ChildProfile NewProfile(int activeProfilesOfParent = 0) =>
        ChildProfile.Create(ParentId, Nickname.From("Bin"), "fox", activeProfilesOfParent, Now);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_nickname_cannot_be_blank(string? value)
    {
        Assert.Throws<DomainException>(() => Nickname.From(value));
    }

    [Fact]
    public void A_nickname_is_trimmed_and_has_at_most_50_characters()
    {
        Assert.Equal("Bông", Nickname.From("  Bông ").Value);
        Assert.Equal(50, Nickname.From(new string('a', 50)).Value.Length);
        Assert.Throws<DomainException>(() => Nickname.From(new string('a', 51)));
    }

    [Fact]
    public void A_new_profile_is_active_and_belongs_to_its_parent()
    {
        var profile = NewProfile();

        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(ChildProfileStatus.Active, profile.Status);
        Assert.True(profile.IsOwnedBy(ParentId));
        Assert.False(profile.IsOwnedBy(Guid.NewGuid()));
        Assert.Equal("Bin", profile.Nickname.Value);
        Assert.Equal(Now, profile.CreatedAtUtc);
    }

    [Fact]
    public void A_parent_can_have_at_most_three_active_profiles()
    {
        Assert.Equal(3, ChildProfile.MaxActivePerParent);
        NewProfile(activeProfilesOfParent: 2);

        Assert.Throws<DomainException>(() => NewProfile(activeProfilesOfParent: 3));
    }

    [Theory]
    [InlineData("dragon")]
    [InlineData("")]
    [InlineData("Fox")]
    public void Only_the_drawn_avatars_can_be_used(string avatarKey)
    {
        Assert.Throws<DomainException>(() => ChildProfile.Create(ParentId, Nickname.From("Bin"), avatarKey, 0, Now));
    }

    [Fact]
    public void An_active_profile_can_change_its_nickname_and_avatar()
    {
        var profile = NewProfile();

        profile.Update(Nickname.From("Bông"), "owl");

        Assert.Equal("Bông", profile.Nickname.Value);
        Assert.Equal("owl", profile.AvatarKey);
    }

    [Fact]
    public void An_archived_profile_cannot_be_changed_until_restored()
    {
        var profile = NewProfile();
        profile.Archive(Now.AddDays(1));

        Assert.Equal(ChildProfileStatus.Archived, profile.Status);
        Assert.Equal(Now.AddDays(1), profile.ArchivedAtUtc);
        Assert.Throws<DomainException>(() => profile.Update(Nickname.From("Bông"), "owl"));

        profile.Restore(activeProfilesOfParent: 0);

        Assert.True(profile.IsActive);
        Assert.Null(profile.ArchivedAtUtc);
    }

    [Fact]
    public void Archiving_and_restoring_twice_change_nothing()
    {
        var profile = NewProfile();
        profile.Archive(Now);
        profile.Archive(Now.AddDays(1));
        Assert.Equal(Now, profile.ArchivedAtUtc);

        profile.Restore(activeProfilesOfParent: 0);
        profile.Restore(activeProfilesOfParent: 3);
        Assert.True(profile.IsActive);
    }

    [Fact]
    public void Only_an_active_profile_can_learn()
    {
        var profile = NewProfile();
        profile.EnsureCanLearn();

        profile.Archive(Now);

        Assert.Throws<DomainException>(profile.EnsureCanLearn);
    }

    [Fact]
    public void Restoring_counts_against_the_limit()
    {
        var profile = NewProfile();
        profile.Archive(Now);

        Assert.Throws<DomainException>(() => profile.Restore(activeProfilesOfParent: 3));
        Assert.False(profile.IsActive);
    }
}
