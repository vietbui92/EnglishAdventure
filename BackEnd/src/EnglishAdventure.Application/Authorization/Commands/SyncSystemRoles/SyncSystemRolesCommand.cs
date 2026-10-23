using MediatR;

namespace EnglishAdventure.Application.Authorization.Commands.SyncSystemRoles;

/// <summary>
/// Brings the database in line with the code catalog: adds missing permissions, creates missing system roles and
/// grants them their default permissions. Never removes anything, so roles customized at runtime are kept.
/// Safe to run any number of times.
/// </summary>
public sealed record SyncSystemRolesCommand : IRequest<SyncSystemRolesResult>;

public sealed record SyncSystemRolesResult(int PermissionsAdded, int RolesCreated, int GrantsAdded);
