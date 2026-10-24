using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Content;

/// <summary>The columns levels and lessons share (<see cref="OrderedContent"/>).</summary>
internal static class OrderedContentMapping
{
    public const string RowVersion = "RowVersion";

    /// <summary>Positions are unique among the non-archived items only: archived ones leave the order.</summary>
    public static readonly string NotArchivedFilter = $"[Status] <> {(int)ContentStatus.Archived}";

    public static void MapOrderedContent<T>(this EntityTypeBuilder<T> builder, string tableName)
        where T : OrderedContent
    {
        var statusValues = string.Join(", ", Enum.GetValues<ContentStatus>().Select(value => (int)value));

        builder.ToTable(tableName, table =>
        {
            table.HasCheckConstraint($"CK_{tableName}_Status", $"[Status] IN ({statusValues})");
            table.HasCheckConstraint($"CK_{tableName}_SortOrder", "[SortOrder] >= 1");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Title)
            .HasConversion(title => title.Value, value => ContentTitle.From(value))
            .HasMaxLength(ContentTitle.MaxLength)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property<byte[]>(RowVersion)
            .IsRowVersion();
    }
}
