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

        // Family revocation filters by FamilyId; the FK index on AccountId supports per-account queries and cascade.
        builder.HasIndex(token => token.FamilyId);

        builder.HasIndex(token => token.AccountId);

        builder.HasIndex(token => token.ExpiresAtUtc);

        builder.Property(token => token.RowVersion)
            .IsRowVersion();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(token => token.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
