using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Roles;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Permissions;
using Microsoft.EntityFrameworkCore;

namespace ITBees.UserManager.Services.Permissions;

/// <summary>
/// Catalog for the admin panel: the keys declared in code, named and grouped by their database rows (seeded by
/// migrations), with the roles holding each of them.
/// </summary>
public class PermissionCatalogService : IPermissionCatalogService
{
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IReadOnlyRepository<Permission> _permissionRoRepo;
    private readonly IReadOnlyRepository<RolePermission> _rolePermissionRoRepo;
    private readonly IReadOnlyRepository<FasIdentityRole> _identityRoleRoRepo;
    private readonly IAspCurrentUserService _aspCurrentUserService;

    public PermissionCatalogService(IPermissionCatalog permissionCatalog,
        IReadOnlyRepository<Permission> permissionRoRepo,
        IReadOnlyRepository<RolePermission> rolePermissionRoRepo,
        IReadOnlyRepository<FasIdentityRole> identityRoleRoRepo,
        IAspCurrentUserService aspCurrentUserService)
    {
        _permissionCatalog = permissionCatalog;
        _permissionRoRepo = permissionRoRepo;
        _rolePermissionRoRepo = rolePermissionRoRepo;
        _identityRoleRoRepo = identityRoleRoRepo;
        _aspCurrentUserService = aspCurrentUserService;
    }

    public PermissionCatalogVm Get()
    {
        if (_aspCurrentUserService.CurrentUserIsPlatformOperator() == false)
            throw new Authorization403ForbiddenException("Only a platform operator can read the permission catalog.");

        var storedPermissions = _permissionRoRepo.GetDataQueryable(x => true)
            .AsNoTracking()
            .ToDictionary(x => x.Key, StringComparer.Ordinal);
        var roleNames = _identityRoleRoRepo.GetData(x => true).ToDictionary(x => x.Id, x => x.Name);
        var grants = _rolePermissionRoRepo.GetDataQueryable(x => true)
            .AsNoTracking()
            .Select(x => new { x.RoleId, x.PermissionKey })
            .ToList()
            .Where(x => roleNames.ContainsKey(x.RoleId))
            .ToList();

        var fullAccessRoleNames = grants
            .Where(x => x.PermissionKey == RolePermission.FullAccessKey)
            .Select(x => roleNames[x.RoleId])
            .Concat(roleNames.Values.Where(x => string.Equals(x, Role.PlatformOperator, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        var rolesByKey = grants
            .GroupBy(x => x.PermissionKey)
            .ToDictionary(x => x.Key, x => x.Select(g => roleNames[g.RoleId]).OrderBy(n => n).ToList());

        IEnumerable<string> RolesOf(string key) =>
            rolesByKey.TryGetValue(key, out var names) ? names : Enumerable.Empty<string>();

        // Keys without a database row (no migration seeded them yet) are listed last, still assignable.
        var permissions = _permissionCatalog.AllKeys
            .Select((key, index) => storedPermissions.TryGetValue(key, out var stored)
                ? (Vm: new PermissionVm(stored, RolesOf(key)), Order: (stored.SortOrder, index))
                : (Vm: new PermissionVm(key, RolesOf(key)), Order: (int.MaxValue, index)))
            .OrderBy(x => x.Order)
            .Select(x => x.Vm);

        return new PermissionCatalogVm(permissions, fullAccessRoleNames);
    }
}
