using EnglishAdventure.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Identity.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        var accountTypeValues = string.Join(", ", Enum.GetValues<AccountType>().Select(value => (int)value));

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AspNetUsers_AccountType",
            $"[AccountType] IN ({accountTypeValues})"));

        builder.Property(user => user.AccountType)
            .HasConversion<int>()
            .IsRequired();

        // No database default: with a CLR default of false, EF would treat an explicit IsActive = false as
        // "unset" and insert the database default instead.
        builder.Property(user => user.IsActive)
            .IsRequired();

        // Identity looks users up by NormalizedEmail; make that lookup unique (Identity's default index is not).
        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL");
    }
}
