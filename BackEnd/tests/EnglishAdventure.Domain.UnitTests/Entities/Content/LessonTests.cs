using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Entities.Content;

public sealed class LessonTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);

    private static Level NewLevel() => Level.Create(ContentTitle.From("Animal Island"), null, sortOrder: 1, Now);

    private static Lesson NewLesson(Level level) => Lesson.Create(level, ContentTitle.From("Pets"), sortOrder: 1, Now);

    /// <summary>A draft lesson that can be published: two words with pictures and a quiz with one question.</summary>
    private static Lesson ReadyLesson(Level level)
    {
        var lesson = NewLesson(level);
        var cat = lesson.AddWord(level, "cat", "con mèo", Picture, Now);
        var dog = lesson.AddWord(level, "dog", "con chó", Picture, Now);
        AddQuiz(level, lesson, cat, dog);
        return lesson;
    }

    private static void AddQuiz(Level level, Lesson lesson, LessonWord correct, LessonWord wrong)
    {
        var quiz = lesson.AddQuiz(level, Now);
        lesson.AddQuizQuestion(
            level,
            quiz.Id,
            QuizQuestionSpec.Create(QuizQuestionForm.PictureToWord, correct.Id, [wrong.Id], null),
            Now);
    }

    private static readonly ImageUrl Picture = ImageUrl.From("https://cdn.example.test/cat.png");

    [Fact]
    public void A_new_lesson_is_a_draft_of_its_level()
    {
        var level = NewLevel();

        var lesson = NewLesson(level);

        Assert.NotEqual(Guid.Empty, lesson.Id);
        Assert.Equal(level.Id, lesson.LevelId);
        Assert.Equal(ContentStatus.Draft, lesson.Status);
        Assert.Equal("Pets", lesson.Title.Value);
    }

    [Fact]
    public void A_lesson_is_published_renamed_and_unpublished_while_no_child_started_it()
    {
        var level = NewLevel();
        var lesson = ReadyLesson(level);

        lesson.Publish(level, Now);
        lesson.Rename(level, ContentTitle.From("My pets"));
        Assert.Equal(ContentStatus.Published, lesson.Status);
        Assert.Equal("My pets", lesson.Title.Value);

        Assert.Throws<DomainException>(() => lesson.Unpublish(level, learnersStarted: true));
        lesson.Unpublish(level, learnersStarted: false);
        Assert.Equal(ContentStatus.Draft, lesson.Status);
    }

    [Fact]
    public void An_archived_level_freezes_its_lessons_but_they_can_still_be_archived()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        level.Archive(Now);

        Assert.Throws<DomainException>(() => NewLesson(level));
        Assert.Throws<DomainException>(() => lesson.Publish(level, Now));
        Assert.Throws<DomainException>(() => lesson.Rename(level, ContentTitle.From("X")));
        Assert.Throws<DomainException>(() => lesson.MoveTo(level, 2));

        lesson.Archive(Now);
        Assert.Equal(ContentStatus.Archived, lesson.Status);
    }

    [Fact]
    public void An_archived_lesson_cannot_change()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        lesson.Archive(Now);

        Assert.Throws<DomainException>(() => lesson.Publish(level, Now));
        Assert.Throws<DomainException>(() => lesson.Rename(level, ContentTitle.From("X")));
        Assert.Throws<DomainException>(() => lesson.MoveTo(level, 2));
    }

    [Fact]
    public void A_lesson_is_changed_only_with_its_own_level()
    {
        var lesson = NewLesson(NewLevel());

        Assert.Throws<DomainException>(() => lesson.Publish(NewLevel(), Now));
    }

    [Fact]
    public void Words_are_added_at_the_end_trimmed_and_mark_the_lesson_changed()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);

        lesson.AddWord(level, "  cat ", " con mèo ", null, Now);
        lesson.AddWord(level, "dog", "con chó", Picture, Now.AddMinutes(1));

        Assert.Equal(["cat", "dog"], lesson.Words.Select(word => word.English));
        Assert.Equal([1, 2], lesson.Words.Select(word => word.SortOrder));
        Assert.Equal("con mèo", lesson.Words[0].Meaning);
        Assert.Null(lesson.Words[0].ImageUrl);
        Assert.Equal(Now.AddMinutes(1), lesson.ContentChangedAtUtc);
    }

    [Theory]
    [InlineData("", "con mèo")]
    [InlineData("cat", " ")]
    public void A_word_needs_its_english_text_and_meaning(string english, string meaning)
    {
        var level = NewLevel();

        Assert.Throws<DomainException>(() => NewLesson(level).AddWord(level, english, meaning, null, Now));
    }

    [Fact]
    public void Texts_have_their_maximum_length()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);

        lesson.AddWord(level, new string('a', 50), new string('b', 100), null, Now);
        Assert.Throws<DomainException>(() => lesson.AddWord(level, new string('c', 51), "x", null, Now));
        Assert.Throws<DomainException>(() => lesson.AddWord(level, "c", new string('x', 101), null, Now));
    }

    [Fact]
    public void A_lesson_has_each_english_word_once_in_any_letter_case()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        lesson.AddWord(level, "Cat", "con mèo", null, Now);
        var dog = lesson.AddWord(level, "dog", "con chó", null, Now);

        var added = Assert.Throws<DomainException>(() => lesson.AddWord(level, " cat", "mèo", null, Now));
        var renamed = Assert.Throws<DomainException>(() => lesson.UpdateWord(level, dog.Id, "CAT", "x", null, Now));
        Assert.Equal(ContentErrorCodes.DuplicateWord, added.Code);
        Assert.Equal(ContentErrorCodes.DuplicateWord, renamed.Code);

        // A word may keep its own text with another letter case.
        lesson.UpdateWord(level, dog.Id, "Dog", "con chó", null, Now);
        Assert.Equal("Dog", lesson.Words[1].English);
    }

    [Fact]
    public void A_lesson_has_at_most_twelve_words()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        for (var index = 0; index < Lesson.MaxWords; index++)
        {
            lesson.AddWord(level, $"word {index}", "nghĩa", null, Now);
        }

        var error = Assert.Throws<DomainException>(() => lesson.AddWord(level, "one more", "nghĩa", null, Now));

        Assert.Equal(12, Lesson.MaxWords);
        Assert.Equal(ContentErrorCodes.WordLimit, error.Code);
    }

    [Fact]
    public void A_lesson_is_published_only_with_words_that_all_have_pictures()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);

        var empty = Assert.Throws<DomainException>(() => lesson.Publish(level, Now));
        var word = lesson.AddWord(level, "cat", "con mèo", null, Now);
        var noPicture = Assert.Throws<DomainException>(() => lesson.Publish(level, Now));
        lesson.UpdateWord(level, word.Id, "cat", "con mèo", Picture, Now);
        AddQuiz(level, lesson, word, lesson.AddWord(level, "dog", "con chó", Picture, Now));
        lesson.Publish(level, Now);

        Assert.Equal(ContentErrorCodes.LessonNotReady, empty.Code);
        Assert.Equal(ContentErrorCodes.LessonNotReady, noPicture.Code);
        Assert.Equal(ContentStatus.Published, lesson.Status);
    }

    [Fact]
    public void A_published_lesson_corrects_words_but_does_not_add_remove_or_reorder_them()
    {
        var level = NewLevel();
        var lesson = ReadyLesson(level);
        var cat = lesson.Words[0];
        lesson.Publish(level, Now);

        lesson.UpdateWord(level, cat.Id, "cats", "những con mèo", ImageUrl.From("https://cdn.example.test/cats.png"), Now);
        var add = Assert.Throws<DomainException>(() => lesson.AddWord(level, "dog", "con chó", Picture, Now));
        var remove = Assert.Throws<DomainException>(() => lesson.RemoveWord(level, cat.Id, Now));
        var reorder = Assert.Throws<DomainException>(() => lesson.ReorderWords(level, [cat.Id], Now));
        var dropPicture = Assert.Throws<DomainException>(() => lesson.UpdateWord(level, cat.Id, "cats", "mèo", null, Now));

        Assert.Equal("cats", lesson.Words[0].English);
        Assert.Equal(ContentErrorCodes.StructureLocked, add.Code);
        Assert.Equal(ContentErrorCodes.StructureLocked, remove.Code);
        Assert.Equal(ContentErrorCodes.StructureLocked, reorder.Code);
        Assert.Equal(ContentErrorCodes.LessonNotReady, dropPicture.Code);
    }

    [Fact]
    public void Removing_and_reordering_keep_positions_without_gaps()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        var cat = lesson.AddWord(level, "cat", "con mèo", null, Now);
        var dog = lesson.AddWord(level, "dog", "con chó", null, Now);
        var fish = lesson.AddWord(level, "fish", "con cá", null, Now);

        lesson.RemoveWord(level, dog.Id, Now);
        Assert.Equal(["cat", "fish"], lesson.Words.Select(word => word.English));
        Assert.Equal([1, 2], lesson.Words.Select(word => word.SortOrder));

        lesson.ReorderWords(level, [fish.Id, cat.Id], Now);
        Assert.Equal(["fish", "cat"], lesson.Words.Select(word => word.English));
        Assert.Throws<DomainException>(() => lesson.ReorderWords(level, [fish.Id], Now));
        Assert.Throws<DomainException>(() => lesson.ReorderWords(level, [fish.Id, fish.Id], Now));
    }

    [Fact]
    public void An_archived_lesson_or_level_freezes_the_word_bank()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        var cat = lesson.AddWord(level, "cat", "con mèo", null, Now);
        lesson.Archive(Now);

        var error = Assert.Throws<DomainException>(() => lesson.UpdateWord(level, cat.Id, "cats", "mèo", null, Now));
        Assert.Equal(ContentErrorCodes.Archived, error.Code);

        var otherLevel = NewLevel();
        var other = NewLesson(otherLevel);
        otherLevel.Archive(Now);
        Assert.Equal(
            ContentErrorCodes.Archived,
            Assert.Throws<DomainException>(() => other.AddWord(otherLevel, "dog", "con chó", null, Now)).Code);
    }

    [Fact]
    public void New_words_are_learn_cards_and_the_cards_follow_the_word_order()
    {
        var level = NewLevel();
        var lesson = NewLesson(level);
        var cat = lesson.AddWord(level, "cat", "con mèo", Picture, Now);
        var dog = lesson.AddWord(level, "dog", "con chó", Picture, Now);
        var fish = lesson.AddWord(level, "fish", "con cá", Picture, Now);

        lesson.SetLearnCard(level, dog.Id, show: false, Now.AddMinutes(5));
        Assert.Equal(Now.AddMinutes(5), lesson.ContentChangedAtUtc);

        // Setting the current value again is no change.
        lesson.SetLearnCard(level, dog.Id, show: false, Now.AddMinutes(9));
        Assert.Equal(Now.AddMinutes(5), lesson.ContentChangedAtUtc);

        lesson.ReorderWords(level, [fish.Id, dog.Id, cat.Id], Now.AddMinutes(10));

        Assert.True(cat.ShowInLearnStep);
        Assert.Equal(["fish", "cat"], lesson.LearnCards.Select(word => word.English));
    }

    [Fact]
    public void Learn_cards_change_only_in_a_draft()
    {
        var level = NewLevel();
        var lesson = ReadyLesson(level);
        var cat = lesson.Words[0];
        lesson.Publish(level, Now);

        var error = Assert.Throws<DomainException>(() => lesson.SetLearnCard(level, cat.Id, show: false, Now));

        Assert.Equal(ContentErrorCodes.StructureLocked, error.Code);
        Assert.True(cat.ShowInLearnStep);
    }

    [Fact]
    public void A_lesson_needs_a_learn_card_to_be_published()
    {
        var level = NewLevel();
        var lesson = ReadyLesson(level);
        foreach (var word in lesson.Words)
        {
            lesson.SetLearnCard(level, word.Id, show: false, Now);
        }

        var error = Assert.Throws<DomainException>(() => lesson.Publish(level, Now));
        lesson.SetLearnCard(level, lesson.Words[0].Id, show: true, Now);
        lesson.Publish(level, Now);

        Assert.Equal(ContentErrorCodes.LessonNotReady, error.Code);
        Assert.Equal(ContentStatus.Published, lesson.Status);
    }

    [Theory]
    [InlineData("https://cdn.example.test/cat.png", true)]
    [InlineData("  https://cdn.example.test/cat.png ", true)]
    [InlineData("http://cdn.example.test/cat.png", false)]
    [InlineData("cdn.example.test/cat.png", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("", false)]
    public void A_picture_address_must_be_https(string value, bool valid)
    {
        Assert.Equal(valid, ImageUrl.IsValid(value));
        Assert.Equal(valid, ImageUrl.IsValid(value) && ImageUrl.From(value).Value == value.Trim());
    }

    [Fact]
    public void A_picture_address_has_at_most_2000_characters()
    {
        var prefix = "https://cdn.example.test/";

        Assert.True(ImageUrl.IsValid(prefix + new string('a', 2000 - prefix.Length)));
        Assert.False(ImageUrl.IsValid(prefix + new string('a', 2001 - prefix.Length)));
        Assert.Null(ImageUrl.FromOptional("  "));
    }
}
