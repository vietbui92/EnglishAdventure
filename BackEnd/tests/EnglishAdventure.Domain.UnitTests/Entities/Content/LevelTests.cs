using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Content;

public sealed class LevelTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);

    private static Level NewLevel(string? description = null) =>
        Level.Create(ContentTitle.From("Animal Island"), description, sortOrder: 1, Now);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_title_cannot_be_blank(string? value)
    {
        Assert.Throws<DomainException>(() => ContentTitle.From(value));
    }

    [Fact]
    public void A_title_is_trimmed_and_has_at_most_100_characters()
    {
        Assert.Equal("Animal Island", ContentTitle.From("  Animal Island ").Value);
        Assert.Equal(100, ContentTitle.From(new string('a', 100)).Value.Length);
        Assert.Throws<DomainException>(() => ContentTitle.From(new string('a', 101)));
    }

    [Fact]
    public void A_new_level_is_a_draft_at_its_position()
    {
        var level = NewLevel();

        Assert.NotEqual(Guid.Empty, level.Id);
        Assert.Equal(ContentStatus.Draft, level.Status);
        Assert.Equal(1, level.SortOrder);
        Assert.Equal(Now, level.CreatedAtUtc);
        Assert.Null(level.PublishedAtUtc);
        Assert.Throws<DomainException>(() => Level.Create(ContentTitle.From("X"), null, sortOrder: 0, Now));
    }

    [Fact]
    public void A_description_is_trimmed_blank_means_none_and_has_at_most_500_characters()
    {
        Assert.Equal("Cats and dogs", NewLevel("  Cats and dogs ").Description);
        Assert.Null(NewLevel("   ").Description);
        Assert.Equal(500, NewLevel(new string('a', 500)).Description!.Length);
        Assert.Throws<DomainException>(() => NewLevel(new string('a', 501)));
    }

    [Fact]
    public void Publishing_and_unpublishing_switch_between_draft_and_published()
    {
        var level = NewLevel();

        level.Publish(Now.AddHours(1));
        Assert.Equal(ContentStatus.Published, level.Status);
        Assert.Equal(Now.AddHours(1), level.PublishedAtUtc);

        // Publishing again keeps the first publication time.
        level.Publish(Now.AddHours(2));
        Assert.Equal(Now.AddHours(1), level.PublishedAtUtc);

        level.Unpublish(learnersStarted: false);
        Assert.Equal(ContentStatus.Draft, level.Status);
        Assert.Null(level.PublishedAtUtc);

        // Unpublishing a draft changes nothing, even when learners started.
        level.Unpublish(learnersStarted: true);
        Assert.Equal(ContentStatus.Draft, level.Status);
    }

    [Fact]
    public void A_level_that_children_started_cannot_be_unpublished()
    {
        var level = NewLevel();
        level.Publish(Now);

        Assert.Throws<DomainException>(() => level.Unpublish(learnersStarted: true));
        Assert.Equal(ContentStatus.Published, level.Status);
    }

    [Fact]
    public void Texts_can_be_corrected_while_published()
    {
        var level = NewLevel();
        level.Publish(Now);

        level.Update(ContentTitle.From("Animal Islands"), "Fixed");

        Assert.Equal("Animal Islands", level.Title.Value);
        Assert.Equal("Fixed", level.Description);
    }

    [Fact]
    public void Archiving_is_final()
    {
        var level = NewLevel();
        level.Publish(Now);
        level.Archive(Now.AddDays(1));
        level.Archive(Now.AddDays(2));

        Assert.Equal(ContentStatus.Archived, level.Status);
        Assert.Equal(Now.AddDays(1), level.ArchivedAtUtc);
        Assert.Throws<DomainException>(() => level.Publish(Now));
        Assert.Throws<DomainException>(() => level.Unpublish(learnersStarted: false));
        Assert.Throws<DomainException>(() => level.Update(ContentTitle.From("X"), null));
        Assert.Throws<DomainException>(() => level.MoveTo(2));
        Assert.Throws<DomainException>(level.EnsureLessonsCanChange);
    }

    [Fact]
    public void A_level_moves_to_a_position_starting_at_one()
    {
        var level = NewLevel();

        level.MoveTo(3);

        Assert.Equal(3, level.SortOrder);
        Assert.Throws<DomainException>(() => level.MoveTo(0));
    }
}
