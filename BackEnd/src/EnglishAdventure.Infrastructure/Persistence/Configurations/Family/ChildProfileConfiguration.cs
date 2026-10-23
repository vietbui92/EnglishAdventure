using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Family;

internal sealed class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    // Case- and width-insensitive, accent-sensitive: "An" and "an" are the same nickname, "An" and "Ân" are not.
    // Set on the column so the family rule does not depend on the server's default collation.
    public const string NicknameCollation = "Latin1_General_100_CI_AS";

    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        var statusValues = string.Join(", ", Enum.GetValues<ChildProfileStatus>().Select(value => (int)value));

        builder.ToTable("family_child_profiles", table => table.HasCheckConstraint(
            "CK_family_child_profiles_Status",
            $"[Status] IN ({statusValues})"));

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Nickname)
            .HasConversion(nickname => nickname.Value, value => Nickname.From(value))
            .HasMaxLength(Nickname.MaxLength)
            .UseCollation(NicknameCollation)
            .IsRequired();

        builder.Property(profile => profile.AvatarKey)
            .HasMaxLength(ChildProfile.AvatarKeyMaxLength)
            .IsRequired();

        builder.Property(profile => profile.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        // Every query filters by family, including archived profiles, which the filtered index below does not cover.
        builder.HasIndex(profile => profile.ParentUserId);

        // Unique among a family's active profiles only, so an archived child's nickname can be used again.
        builder.HasIndex(profile => new { profile.ParentUserId, profile.Nickname })
            .IsUnique()
            .HasFilter($"[Status] = {(int)ChildProfileStatus.Active}");

        // Another module's entity: a foreign key without a navigation property (ADR-1). A Parent with children cannot
        // be deleted by accident; removing child data is a privacy decision (S6.4).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(profile => profile.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
