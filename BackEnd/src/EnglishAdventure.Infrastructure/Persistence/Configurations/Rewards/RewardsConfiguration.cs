using EnglishAdventure.Domain.Entities.Content;
using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Entities.Rewards;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Rewards;

internal sealed class PointsTransactionConfiguration : IEntityTypeConfiguration<PointsTransaction>
{
    public void Configure(EntityTypeBuilder<PointsTransaction> builder)
    {
        builder.ToTable("rewards_points_transactions", table =>
        {
            table.HasCheckConstraint("CK_rewards_points_transactions_StarNumber", "[StarNumber] BETWEEN 1 AND 3");
            table.HasCheckConstraint("CK_rewards_points_transactions_Points", "[Points] > 0");
        });

        builder.HasKey(transaction => transaction.Id);

        // The source key: each star of a lesson is paid once per child (PD-004, PD-011).
        builder.HasIndex(transaction => new { transaction.ChildProfileId, transaction.LessonId, transaction.StarNumber })
            .IsUnique();

        // Other modules' entities: foreign keys without navigation properties (ADR-1). Rewards are kept.
        builder.HasOne<ChildProfile>()
            .WithMany()
            .HasForeignKey(transaction => transaction.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(transaction => transaction.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BadgeDefinitionConfiguration : IEntityTypeConfiguration<BadgeDefinition>
{
    public void Configure(EntityTypeBuilder<BadgeDefinition> builder)
    {
        var kinds = string.Join(", ", Enum.GetValues<BadgeKind>().Select(kind => (int)kind));
        builder.ToTable("rewards_badges", table =>
            table.HasCheckConstraint("CK_rewards_badges_Kind", $"[Kind] IN ({kinds})"));

        builder.HasKey(badge => badge.Id);

        builder.Property(badge => badge.Kind)
            .HasConversion<int>();
        builder.HasIndex(badge => badge.Kind)
            .IsUnique();

        builder.Property(badge => badge.Name)
            .HasMaxLength(BadgeDefinition.NameMaxLength)
            .IsRequired();

        builder.Property(badge => badge.Description)
            .HasMaxLength(BadgeDefinition.DescriptionMaxLength)
            .IsRequired();

        builder.Property(badge => badge.ImageUrl)
            .HasConversion(url => url!.Value, value => ImageUrl.From(value))
            .HasMaxLength(ImageUrl.MaxLength);
    }
}

internal sealed class BadgeAwardConfiguration : IEntityTypeConfiguration<BadgeAward>
{
    public void Configure(EntityTypeBuilder<BadgeAward> builder)
    {
        builder.ToTable("rewards_badge_awards");

        builder.HasKey(award => award.Id);

        builder.Property(award => award.Kind)
            .HasConversion<int>();

        // Each badge once per child, and per level for the level badges (no filter: SQL Server then treats the null
        // levels of the other badges as equal, so those are once per child too).
        builder.HasIndex(award => new { award.ChildProfileId, award.Kind, award.LevelId })
            .IsUnique()
            .HasFilter(null);

        builder.HasOne<ChildProfile>()
            .WithMany()
            .HasForeignKey(award => award.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Level>()
            .WithMany()
            .HasForeignKey(award => award.LevelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(award => award.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
