using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Learning;

internal sealed class LessonCompletionConfiguration : IEntityTypeConfiguration<LessonCompletion>
{
    public void Configure(EntityTypeBuilder<LessonCompletion> builder)
    {
        builder.ToTable("learning_lesson_completions", table =>
        {
            table.HasCheckConstraint("CK_learning_lesson_completions_BestStars", "[BestStars] BETWEEN 1 AND 3");
            table.HasCheckConstraint(
                "CK_learning_lesson_completions_BestScorePercent",
                "[BestScorePercent] BETWEEN 0 AND 100");
        });

        builder.HasKey(completion => completion.Id);

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        // One per child and lesson: two finishes at once cannot both create it (S4.2).
        builder.HasIndex(completion => new { completion.ChildProfileId, completion.LessonId })
            .IsUnique();

        // Other modules' entities: foreign keys without navigation properties (ADR-1). Learning records are kept.
        builder.HasOne<ChildProfile>()
            .WithMany()
            .HasForeignKey(completion => completion.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(completion => completion.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
