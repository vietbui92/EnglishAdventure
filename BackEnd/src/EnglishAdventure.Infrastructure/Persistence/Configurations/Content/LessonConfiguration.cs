using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Content;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.MapOrderedContent("content_lessons");

        var kindValues = string.Join(", ", Enum.GetValues<LessonKind>().Select(value => (int)value));
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_content_lessons_Kind", $"[Kind] IN ({kindValues})");
            // Only a boss has a boss picture (S3.8).
            table.HasCheckConstraint(
                "CK_content_lessons_BossImageUrl",
                $"[Kind] = {(int)LessonKind.Boss} OR [BossImageUrl] IS NULL");
        });

        builder.Property(lesson => lesson.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(lesson => lesson.BossImageUrl)
            .HasConversion(url => url!.Value, value => ImageUrl.From(value))
            .HasMaxLength(ImageUrl.MaxLength);

        // Every lesson list filters by level, including archived lessons, which the filtered indexes below do not cover.
        builder.HasIndex(lesson => lesson.LevelId);

        // Positions are unique among the level's regular lessons; the boss is not in the order (S3.8).
        builder.HasIndex(lesson => new { lesson.LevelId, lesson.SortOrder })
            .IsUnique()
            .HasFilter($"{OrderedContentMapping.NotArchivedFilter} AND [Kind] = {(int)LessonKind.Regular}");

        // At most one boss per level that is not archived. Named, so it is a second index on LevelId and not the one above.
        builder.HasIndex(lesson => lesson.LevelId, "IX_content_lessons_LevelId_Boss")
            .IsUnique()
            .HasFilter($"{OrderedContentMapping.NotArchivedFilter} AND [Kind] = {(int)LessonKind.Boss}");

        // The word bank belongs to the lesson and is loaded and saved with it.
        builder.HasMany(lesson => lesson.Words)
            .WithOne()
            .HasForeignKey(word => word.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(lesson => lesson.Words)
            .HasField("_words")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Computed from the words (S3.7), not a second relationship.
        builder.Ignore(lesson => lesson.LearnCards);

        // The activities (S3.2) also belong to the lesson and are loaded and saved with it.
        builder.HasMany(lesson => lesson.Activities)
            .WithOne()
            .HasForeignKey(activity => activity.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(lesson => lesson.Activities)
            .HasField("_activities")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Another aggregate: a foreign key without a navigation property. Levels are archived, never deleted.
        builder.HasOne<Level>()
            .WithMany()
            .HasForeignKey(lesson => lesson.LevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
