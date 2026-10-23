using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.UnitTests.Entities.Identity;

public sealed class AccountTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_trims_and_normalizes_the_email_and_starts_active()
    {
        var account = Account.Create("  Parent@Example.com ", "hash", AccountType.Parent, Now);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("Parent@Example.com", account.Email);
        Assert.Equal("PARENT@EXAMPLE.COM", account.NormalizedEmail);
        Assert.True(account.IsActive);
        Assert.Equal(Now, account.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    public void Create_rejects_an_invalid_email(string email)
    {
        Assert.Throws<DomainException>(() => Account.Create(email, "hash", AccountType.Parent, Now));
    }

    [Fact]
    public void Create_rejects_a_missing_password_hash_and_an_unknown_account_type()
    {
        Assert.Throws<DomainException>(() => Account.Create("a@b.test", " ", AccountType.Parent, Now));
        Assert.Throws<DomainException>(() => Account.Create("a@b.test", "hash", (AccountType)99, Now));
    }

    [Fact]
    public void The_fifth_failed_login_locks_the_account_for_the_lockout_duration()
    {
        var account = Account.Create("a@b.test", "hash", AccountType.Parent, Now);

        for (var attempt = 1; attempt < Account.MaxFailedLoginAttempts; attempt++)
        {
            account.RecordFailedLogin(Now);
        }

        Assert.False(account.IsLockedOut(Now));

        account.RecordFailedLogin(Now);

        Assert.True(account.IsLockedOut(Now));
        Assert.True(account.IsLockedOut(Now.Add(Account.LockoutDuration).AddSeconds(-1)));
        Assert.False(account.IsLockedOut(Now.Add(Account.LockoutDuration)));
        Assert.Equal(0, account.FailedLoginCount);
    }

    [Fact]
    public void A_successful_login_resets_failures_and_records_the_time()
    {
        var account = Account.Create("a@b.test", "hash", AccountType.Parent, Now);
        account.RecordFailedLogin(Now);

        account.RecordSuccessfulLogin(Now.AddMinutes(1));

        Assert.Equal(0, account.FailedLoginCount);
        Assert.Null(account.LockoutEndUtc);
        Assert.Equal(Now.AddMinutes(1), account.LastLoginAtUtc);
    }

    [Fact]
    public void An_inactive_account_cannot_record_a_successful_login()
    {
        var account = Account.Create("a@b.test", "hash", AccountType.Child, Now);
        account.Deactivate();

        Assert.Throws<DomainException>(() => account.RecordSuccessfulLogin(Now));
    }

    [Fact]
    public void Assigning_the_same_role_twice_keeps_one_assignment_and_it_can_be_removed()
    {
        var account = Account.Create("a@b.test", "hash", AccountType.Admin, Now);
        var roleId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        account.AssignRole(roleId, Now, assignedBy);
        account.AssignRole(roleId, Now.AddMinutes(1));

        var assignment = Assert.Single(account.Roles);
        Assert.Equal(account.Id, assignment.AccountId);
        Assert.Equal(roleId, assignment.RoleId);
        Assert.Equal(Now, assignment.AssignedAtUtc);
        Assert.Equal(assignedBy, assignment.AssignedByAccountId);

        account.RemoveRole(roleId);

        Assert.Empty(account.Roles);
    }
}
