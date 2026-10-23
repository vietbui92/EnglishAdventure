using EnglishAdventure.Application.Common.Persistence;
using EnglishAdventure.Domain.Entities.Identity;
using MediatR;

namespace EnglishAdventure.Application.Authorization.Commands.SyncSystemRoles;

public sealed class SyncSystemRolesCommandHandler : IRequestHandler<SyncSystemRolesCommand, SyncSystemRolesResult>
{
    private readonly IPermissionRepository _permissions;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public SyncSystemRolesCommandHandler(
        IPermissionRepository permissions,
        IRoleRepository roles,
        IUnitOfWork unitOfWork)
    {
        _permissions = permissions;
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task<SyncSystemRolesResult> Handle(SyncSystemRolesCommand request, CancellationToken cancellationToken)
    {
        var storedPermissions = (await _permissions.ListAsync(cancellationToken))
            .ToDictionary(permission => permission.Code, StringComparer.Ordinal);

        var permissionsAdded = 0;
        foreach (var definition in Permissions.All)
        {
            if (storedPermissions.TryGetValue(definition.Code, out var stored))
            {
                stored.Describe(definition.Description);
                continue;
            }

            var permission = Permission.Create(definition.Code, definition.Description);
            _permissions.Add(permission);
            storedPermissions.Add(permission.Code, permission);
            permissionsAdded++;
        }

        var rolesCreated = 0;
        var grantsAdded = 0;
        foreach (var definition in SystemRoles.All)
        {
            var role = await _roles.GetByNameAsync(definition.Name, cancellationToken);
            if (role is null)
            {
                role = Role.Create(definition.Name, definition.Description);
                _roles.Add(role);
                rolesCreated++;
            }

            foreach (var code in definition.Permissions)
            {
                var permissionId = storedPermissions[code].Id;
                if (!role.HasPermission(permissionId))
                {
                    role.GrantPermission(permissionId);
                    grantsAdded++;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SyncSystemRolesResult(permissionsAdded, rolesCreated, grantsAdded);
    }
}
