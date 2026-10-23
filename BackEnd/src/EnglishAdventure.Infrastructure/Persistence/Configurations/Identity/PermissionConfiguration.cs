using EnglishAdventure.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Identity;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("auth_permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Code)
            .HasMaxLength(Permission.CodeMaxLength)
            .IsRequired();

        builder.HasIndex(permission => permission.Code)
            .IsUnique();

        builder.Property(permission => permission.Description)
            .HasMaxLength(Permission.DescriptionMaxLength);
    }
}
