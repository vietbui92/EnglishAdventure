using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Entities.Learning;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Learning;

internal sealed class LessonAttemptConfiguration : IEntityTypeConfiguration<LessonAttempt>
{
    public void Configure(EntityTypeBuilder<LessonAttempt> builder)
    {
        var statusValues = string.Join(", ", Enum.GetValues<AttemptStatus>().Select(value => (int)value));

        builder.ToTable("learning_lesson_attempts", table =>
        {
            table.HasCheckConstraint("CK_learning_lesson_attempts_Status", $"[Status] IN ({statusValues})");
            table.HasCheckConstraint("CK_learning_lesson_attempts_Stars", "[Stars] IS NULL OR [Stars] BETWEEN 0 AND 3");
        });

        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        // A child has at most one unfinished attempt per lesson: coming back resumes it (PD-012).
        builder.HasIndex(attempt => new { attempt.ChildProfileId, attempt.LessonId })
            .IsUnique()
            .HasFilter($"[Status] = {(int)AttemptStatus.InProgress}");

        // "Has a child started this lesson?" (unpublishing, PD-012).
        builder.HasIndex(attempt => attempt.LessonId);

        builder.HasMany(attempt => attempt.Answers)
            .WithOne()
            .HasForeignKey(answer => answer.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(attempt => attempt.Answers)
            .HasField("_answers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Other modules' entities: foreign keys without navigation properties (ADR-1). Learning records are kept.
        builder.HasOne<ChildProfile>()
            .WithMany()
            .HasForeignKey(attempt => attempt.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(attempt => attempt.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        builder.ToTable("learning_attempt_answers", table =>
            table.HasCheckConstraint("CK_learning_attempt_answers_TryNumber", "[TryNumber] >= 1"));

        builder.HasKey(answer => answer.Id);

        // The same try is saved once, even when sent twice at the same moment.
        builder.HasIndex(answer => new { answer.AttemptId, answer.ItemId, answer.TryNumber })
            .IsUnique();
    }
}
