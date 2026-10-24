using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Content;

internal sealed class LessonWordConfiguration : IEntityTypeConfiguration<LessonWord>
{
    // Case-insensitive, accent-sensitive, like the domain's duplicate check: "Cat" and "cat" are the same word.
    public const string EnglishCollation = "Latin1_General_100_CI_AS";

    public void Configure(EntityTypeBuilder<LessonWord> builder)
    {
        builder.ToTable("content_lesson_words", table =>
            table.HasCheckConstraint("CK_content_lesson_words_SortOrder", "[SortOrder] >= 1"));

        builder.HasKey(word => word.Id);

        builder.Property(word => word.English)
            .HasMaxLength(LessonWord.EnglishMaxLength)
            .UseCollation(EnglishCollation)
            .IsRequired();

        builder.Property(word => word.Meaning)
            .HasMaxLength(LessonWord.MeaningMaxLength)
            .IsRequired();

        builder.Property(word => word.ImageUrl)
            .HasConversion(url => url!.Value, value => ImageUrl.From(value))
            .HasMaxLength(ImageUrl.MaxLength);

        builder.Property(word => word.SoundUrl)
            .HasConversion(url => url!.Value, value => SoundUrl.From(value))
            .HasMaxLength(SoundUrl.MaxLength);

        // A backstop for the lesson's own rule. Positions are kept by the lesson (one aggregate, saved together), so they
        // have no unique index that a reorder would have to step around.
        builder.HasIndex(word => new { word.LessonId, word.English })
            .IsUnique();
    }
}
