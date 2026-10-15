using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Roles;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.RestfulApiControllers.Models;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Permissions;

/// <summary>
/// Role = name + permissions. Roles are global (one catalog for the platform), so only a platform operator
/// may change their permissions.
/// </summary>
public class UserRolePermissionsService : IUserRolePermissionsService
{
    private readonly IReadOnlyRepository<FasIdentityRole> _identityRoleRoRepo;
    private readonly IReadOnlyRepository<RolePermission> _rolePermissionRoRepo;
    private readonly IWriteOnlyRepository<RolePermission> _rolePermissionWoRepo;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<UserRolePermissionsService> _logger;

    public UserRolePermissionsService(IReadOnlyRepository<FasIdentityRole> identityRoleRoRepo,
        IReadOnlyRepository<RolePermission> rolePermissionRoRepo,
        IWriteOnlyRepository<RolePermission> rolePermissionWoRepo,
        IPermissionCatalog permissionCatalog,
        IAspCurrentUserService aspCurrentUserService,
        IMemoryCache memoryCache,
        ILogger<UserRolePermissionsService> logger)
    {
        _identityRoleRoRepo = identityRoleRoRepo;
        _rolePermissionRoRepo = rolePermissionRoRepo;
        _rolePermissionWoRepo = rolePermissionWoRepo;
        _permissionCatalog = permissionCatalog;
        _aspCurrentUserService = aspCurrentUserService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public UserRolePermissionsVm Get(Guid roleGuid)
    {
        RequirePlatformOperator();
        var role = GetRole(roleGuid);
        if (IsPlatformOperatorRole(role))
            return new UserRolePermissionsVm(role, true, Array.Empty<string>());

        var storedKeys = GetStoredKeys(role.Id);
        return storedKeys.Contains(RolePermission.FullAccessKey)
            ? new UserRolePermissionsVm(role, true, Array.Empty<string>())
            : new UserRolePermissionsVm(role, false, storedKeys.Where(_permissionCatalog.Contains));
    }

    public UserRolePermissionsVm Update(UserRolePermissionsUm userRolePermissionsUm)
    {
        RequirePlatformOperator();
        var role = GetRole(userRolePermissionsUm.RoleGuid);
        if (IsPlatformOperatorRole(role))
            throw new FasApiErrorException(new FasApiErrorVm(
                "The platform operator role always has every permission.", 400, ""));

        var permissionKeys = userRolePermissionsUm.FullAccess
            ? new List<string> { RolePermission.FullAccessKey }
            : (userRolePermissionsUm.PermissionKeys ?? new List<string>())
                .Where(x => string.IsNullOrWhiteSpace(x) == false)
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

        // The full-access marker is accepted only through FullAccess, never as one of the keys.
        var unknownKeys = userRolePermissionsUm.FullAccess
            ? new List<string>()
            : permissionKeys.Where(x => _permissionCatalog.Contains(x) == false).ToList();
        if (unknownKeys.Count > 0)
            throw new FasApiErrorException(new FasApiErrorVm(
                "Unknown permission keys: " + string.Join(", ", unknownKeys), 400, ""));

        var previousKeys = GetStoredKeys(role.Id);
        _rolePermissionWoRepo.DeleteData(x => x.RoleId == role.Id);
        if (permissionKeys.Count > 0)
        {
            _rolePermissionWoRepo.InsertData(permissionKeys
                .Select(x => new RolePermission { RoleId = role.Id, PermissionKey = x })
                .ToList());
        }
        _memoryCache.Remove(PermissionService.GetRoleCacheKey(role.Id));

        _logger.LogWarning("Permissions of role {RoleName} ({RoleGuid}) changed by {UserEmail}: [{PreviousKeys}] -> [{PermissionKeys}]",
            role.Name, role.Id, _aspCurrentUserService.GetCurrentUser()?.Email,
            string.Join(", ", previousKeys), string.Join(", ", permissionKeys));

        return userRolePermissionsUm.FullAccess
            ? new UserRolePermissionsVm(role, true, Array.Empty<string>())
            : new UserRolePermissionsVm(role, false, permissionKeys);
    }

    private void RequirePlatformOperator()
    {
        if (_aspCurrentUserService.CurrentUserIsPlatformOperator() == false)
            throw new Authorization403ForbiddenException("Only a platform operator can manage role permissions.");
    }

    private FasIdentityRole GetRole(Guid roleGuid)
    {
        var role = _identityRoleRoRepo.GetFirst(x => x.Id == roleGuid);
        if (role == null)
            throw new FasApiErrorException(new FasApiErrorVm("Role not found", 404, ""));
        return role;
    }

    private static bool IsPlatformOperatorRole(FasIdentityRole role)
    {
        return string.Equals(role.Name, Role.PlatformOperator, StringComparison.OrdinalIgnoreCase);
    }

    private List<string> GetStoredKeys(Guid roleId)
    {
        return _rolePermissionRoRepo.GetDataQueryable(x => x.RoleId == roleId)
            .AsNoTracking()
            .Select(x => x.PermissionKey)
            .ToList();
    }
}
