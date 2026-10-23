using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        var userTypeValues = string.Join(", ", Enum.GetValues<UserType>().Select(value => (int)value));

        builder.ToTable("auth_users", table => table.HasCheckConstraint(
            "CK_auth_users_UserType",
            $"[UserType] IN ({userTypeValues})"));

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.ConsentVersion)
            .HasMaxLength(User.ConsentVersionMaxLength);

        builder.Property(user => user.UserType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        builder.HasMany(user => user.Roles)
            .WithOne()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(user => user.Roles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
