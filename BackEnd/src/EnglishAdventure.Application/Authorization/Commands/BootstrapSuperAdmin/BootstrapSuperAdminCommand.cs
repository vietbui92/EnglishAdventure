using EnglishAdventure.Application.Authorization.Commands.SyncSystemRoles;
using MediatR;

namespace EnglishAdventure.Application.Authorization.Commands.BootstrapSuperAdmin;

/// <summary>
/// Creates the very first SuperAdmin, after syncing the system roles. Run once per environment from the command line;
/// refused when an active SuperAdmin already exists. Later SuperAdmins are created by an existing SuperAdmin.
/// </summary>
public sealed record BootstrapSuperAdminCommand(string Email, string Password) : IRequest<BootstrapSuperAdminResult>
{
    // Keeps the password out of logs and exception messages that print the request.
    public override string ToString() => $"{nameof(BootstrapSuperAdminCommand)} {{ Email = {Email} }}";
}

public sealed record BootstrapSuperAdminResult(Guid UserId, SyncSystemRolesResult Sync);
