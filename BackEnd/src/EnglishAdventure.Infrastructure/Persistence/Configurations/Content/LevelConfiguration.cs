using EnglishAdventure.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Content;

internal sealed class LevelConfiguration : IEntityTypeConfiguration<Level>
{
    public void Configure(EntityTypeBuilder<Level> builder)
    {
        builder.MapOrderedContent("content_levels");

        builder.Property(level => level.Description)
            .HasMaxLength(Level.DescriptionMaxLength);

        builder.HasIndex(level => level.SortOrder)
            .IsUnique()
            .HasFilter(OrderedContentMapping.NotArchivedFilter);
    }
}
