using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Identity;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        var accountTypeValues = string.Join(", ", Enum.GetValues<AccountType>().Select(value => (int)value));

        builder.ToTable("auth_accounts", table => table.HasCheckConstraint(
            "CK_auth_accounts_AccountType",
            $"[AccountType] IN ({accountTypeValues})"));

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Email)
            .HasMaxLength(Account.EmailMaxLength)
            .IsRequired();

        builder.Property(account => account.NormalizedEmail)
            .HasMaxLength(Account.EmailMaxLength)
            .IsRequired();

        builder.HasIndex(account => account.NormalizedEmail)
            .IsUnique();

        builder.Property(account => account.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(account => account.AccountType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        builder.HasMany(account => account.Roles)
            .WithOne()
            .HasForeignKey(accountRole => accountRole.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(account => account.Roles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
