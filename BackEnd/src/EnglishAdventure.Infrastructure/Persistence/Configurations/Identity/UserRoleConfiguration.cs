using EnglishAdventure.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("auth_user_roles");

        builder.HasKey(userRole => userRole.Id);

        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleId })
            .IsUnique();

        // A role that is still assigned to a user cannot be deleted.
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userRole => userRole.AssignedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
