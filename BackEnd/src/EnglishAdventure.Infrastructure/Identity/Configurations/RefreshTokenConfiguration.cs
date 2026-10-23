using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Identity.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

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

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
