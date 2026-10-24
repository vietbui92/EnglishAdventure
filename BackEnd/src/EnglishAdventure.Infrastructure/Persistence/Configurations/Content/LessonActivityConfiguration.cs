using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Content;

/// <summary>
/// All activity kinds share one table with a <c>Type</c> column (EF "table per hierarchy"): quizzes now, matching in
/// S3.3. The CLR <see cref="LessonActivity.Type"/> is computed by each kind, so the column is a shadow discriminator.
/// </summary>
internal sealed class LessonActivityConfiguration : IEntityTypeConfiguration<LessonActivity>
{
    private const string Discriminator = "ActivityType";

    public void Configure(EntityTypeBuilder<LessonActivity> builder)
    {
        var typeValues = string.Join(", ", Enum.GetValues<ActivityType>().Select(value => (int)value));
        var modeValues = string.Join(", ", Enum.GetValues<MatchingMode>().Select(value => (int)value));

        builder.ToTable("content_activities", table =>
        {
            table.HasCheckConstraint("CK_content_activities_Type", $"[Type] IN ({typeValues})");
            table.HasCheckConstraint("CK_content_activities_SortOrder", "[SortOrder] >= 1");
            // A matching row has a mode, a quiz row has none (S3.3).
            table.HasCheckConstraint(
                "CK_content_activities_MatchingMode",
                $"([Type] = {(int)ActivityType.Matching} AND [MatchingMode] IN ({modeValues})) OR ([Type] <> {(int)ActivityType.Matching} AND [MatchingMode] IS NULL)");
        });

        builder.HasKey(activity => activity.Id);
        builder.Ignore(activity => activity.Type);

        builder.HasDiscriminator<ActivityType>(Discriminator)
            .HasValue<QuizActivity>(ActivityType.Quiz)
            .HasValue<MatchingActivity>(ActivityType.Matching);
        builder.Property<ActivityType>(Discriminator)
            .HasColumnName("Type")
            .HasConversion<int>();

        builder.HasIndex(activity => activity.LessonId);
    }
}

internal sealed class QuizActivityConfiguration : IEntityTypeConfiguration<QuizActivity>
{
    public void Configure(EntityTypeBuilder<QuizActivity> builder)
    {
        builder.HasMany(quiz => quiz.Questions)
            .WithOne()
            .HasForeignKey(question => question.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(quiz => quiz.Questions)
            .HasField("_questions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MatchingActivityConfiguration : IEntityTypeConfiguration<MatchingActivity>
{
    public void Configure(EntityTypeBuilder<MatchingActivity> builder)
    {
        // A column of the shared activities table, set only for matching rows (check constraint on the base type).
        builder.Property(matching => matching.Mode)
            .HasColumnName("MatchingMode")
            .HasConversion<int>();

        builder.HasMany(matching => matching.Pairs)
            .WithOne()
            .HasForeignKey(pair => pair.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(matching => matching.Pairs)
            .HasField("_pairs")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MatchingPairConfiguration : IEntityTypeConfiguration<MatchingPair>
{
    public void Configure(EntityTypeBuilder<MatchingPair> builder)
    {
        builder.ToTable("content_matching_pairs", table =>
            table.HasCheckConstraint("CK_content_matching_pairs_SortOrder", "[SortOrder] >= 1"));

        builder.HasKey(pair => pair.Id);

        // Each word appears once per matching.
        builder.HasIndex(pair => new { pair.ActivityId, pair.WordId })
            .IsUnique();

        // No cascade: a word used by a matching cannot be removed (S3.3).
        builder.HasOne<LessonWord>()
            .WithMany()
            .HasForeignKey(pair => pair.WordId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        var formValues = string.Join(", ", Enum.GetValues<QuizQuestionForm>().Select(value => (int)value));

        builder.ToTable("content_quiz_questions", table =>
        {
            table.HasCheckConstraint("CK_content_quiz_questions_Form", $"[Form] IN ({formValues})");
            table.HasCheckConstraint("CK_content_quiz_questions_SortOrder", "[SortOrder] >= 1");
        });

        builder.HasKey(question => question.Id);
        builder.Ignore(question => question.Spec);

        builder.Property(question => question.Form)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(question => question.Sentence)
            .HasMaxLength(Domain.ValueObjects.QuizQuestionSpec.SentenceMaxLength);

        builder.HasMany(question => question.Options)
            .WithOne()
            .HasForeignKey(option => option.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(question => question.Options)
            .HasField("_options")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // The answer is a word of the lesson. No cascade: a word used by a question cannot be removed (S3.2).
        builder.HasOne<LessonWord>()
            .WithMany()
            .HasForeignKey(question => question.CorrectWordId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class QuizOptionConfiguration : IEntityTypeConfiguration<QuizOption>
{
    public void Configure(EntityTypeBuilder<QuizOption> builder)
    {
        builder.ToTable("content_quiz_options", table =>
            table.HasCheckConstraint("CK_content_quiz_options_SortOrder", "[SortOrder] >= 1"));

        builder.HasKey(option => option.Id);

        // Each word is offered once per question.
        builder.HasIndex(option => new { option.QuestionId, option.WordId })
            .IsUnique();

        builder.HasOne<LessonWord>()
            .WithMany()
            .HasForeignKey(option => option.WordId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
