using EnglishAdventure.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishAdventure.Infrastructure.Persistence.Configurations.Identity;

internal sealed class AccountRoleConfiguration : IEntityTypeConfiguration<AccountRole>
{
    public void Configure(EntityTypeBuilder<AccountRole> builder)
    {
        builder.ToTable("auth_account_roles");

        builder.HasKey(accountRole => accountRole.Id);

        builder.HasIndex(accountRole => new { accountRole.AccountId, accountRole.RoleId })
            .IsUnique();

        // A role that is still assigned to an account cannot be deleted.
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(accountRole => accountRole.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(accountRole => accountRole.AssignedByAccountId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
