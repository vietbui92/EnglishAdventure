using EnglishAdventure.Domain.Entities.Identity;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;

namespace EnglishAdventure.Domain.UnitTests.Entities.Identity;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_trims_and_normalizes_the_email_and_starts_active()
    {
        var user = User.Create("  Parent@Example.com ", "hash", UserType.Parent, Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Parent@Example.com", user.Email);
        Assert.Equal("PARENT@EXAMPLE.COM", user.NormalizedEmail);
        Assert.True(user.IsActive);
        Assert.Equal(Now, user.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    public void Create_rejects_an_invalid_email(string email)
    {
        Assert.Throws<DomainException>(() => User.Create(email, "hash", UserType.Parent, Now));
    }

    [Fact]
    public void Create_rejects_a_missing_password_hash_and_an_unknown_user_type()
    {
        Assert.Throws<DomainException>(() => User.Create("a@b.test", " ", UserType.Parent, Now));
        Assert.Throws<DomainException>(() => User.Create("a@b.test", "hash", (UserType)99, Now));
    }

    [Fact]
    public void The_fifth_failed_login_locks_the_user_for_the_lockout_duration()
    {
        var user = User.Create("a@b.test", "hash", UserType.Parent, Now);

        for (var attempt = 1; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            user.RecordFailedLogin(Now);
        }

        Assert.False(user.IsLockedOut(Now));

        user.RecordFailedLogin(Now);

        Assert.True(user.IsLockedOut(Now));
        Assert.True(user.IsLockedOut(Now.Add(User.LockoutDuration).AddSeconds(-1)));
        Assert.False(user.IsLockedOut(Now.Add(User.LockoutDuration)));
        Assert.Equal(0, user.FailedLoginCount);
    }

    [Fact]
    public void A_successful_login_resets_failures_and_records_the_time()
    {
        var user = User.Create("a@b.test", "hash", UserType.Parent, Now);
        user.RecordFailedLogin(Now);

        user.RecordSuccessfulLogin(Now.AddMinutes(1));

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockoutEndUtc);
        Assert.Equal(Now.AddMinutes(1), user.LastLoginAtUtc);
    }

    [Fact]
    public void An_inactive_user_cannot_record_a_successful_login()
    {
        var user = User.Create("a@b.test", "hash", UserType.Child, Now);
        user.Deactivate();

        Assert.Throws<DomainException>(() => user.RecordSuccessfulLogin(Now));
    }

    [Fact]
    public void Assigning_the_same_role_twice_keeps_one_assignment_and_it_can_be_removed()
    {
        var user = User.Create("a@b.test", "hash", UserType.Admin, Now);
        var roleId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        user.AssignRole(roleId, Now, assignedBy);
        user.AssignRole(roleId, Now.AddMinutes(1));

        var assignment = Assert.Single(user.Roles);
        Assert.Equal(user.Id, assignment.UserId);
        Assert.Equal(roleId, assignment.RoleId);
        Assert.Equal(Now, assignment.AssignedAtUtc);
        Assert.Equal(assignedBy, assignment.AssignedByUserId);

        user.RemoveRole(roleId);

        Assert.Empty(user.Roles);
    }
}
