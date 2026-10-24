using EnglishAdventure.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Content;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.MapOrderedContent("content_lessons");

        // Every lesson list filters by level, including archived lessons, which the filtered index below does not cover.
        builder.HasIndex(lesson => lesson.LevelId);

        builder.HasIndex(lesson => new { lesson.LevelId, lesson.SortOrder })
            .IsUnique()
            .HasFilter(OrderedContentMapping.NotArchivedFilter);

        // Another aggregate: a foreign key without a navigation property. Levels are archived, never deleted.
        builder.HasOne<Level>()
            .WithMany()
            .HasForeignKey(lesson => lesson.LevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
