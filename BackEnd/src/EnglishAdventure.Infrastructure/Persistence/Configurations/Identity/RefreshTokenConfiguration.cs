using EnglishAdventure.Domain.Entities.Family;
using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Identity;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("auth_refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.RevocationReason)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Family revocation filters by FamilyId; the FK index on UserId supports per-user queries and cascade.
        builder.HasIndex(token => token.FamilyId);

        builder.HasIndex(token => token.UserId);

        builder.HasIndex(token => token.ExpiresAtUtc);

        builder.Property(token => token.RowVersion)
            .IsRowVersion();

        // Another module's entity: a foreign key without a navigation property (ADR-1).
        builder.HasOne<ChildProfile>()
            .WithMany()
            .HasForeignKey(token => token.ChildProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
