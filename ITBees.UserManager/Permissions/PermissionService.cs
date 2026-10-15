using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Roles;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Permissions;

/// <summary>
/// Resolves the effective permissions of the current user in a company:
/// platform operator (global role) and the company owner hold every permission; any other member holds the
/// permissions stored for their company role (RolePermission, managed in the admin panel); a non-member holds nothing.
/// </summary>
public class PermissionService : IPermissionService
{
    private static readonly TimeSpan RolePermissionsCacheTime = TimeSpan.FromSeconds(60);
    private static readonly HashSet<string> NoPermissions = new(StringComparer.Ordinal);

    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
    private readonly IReadOnlyRepository<Company> _companyRoRepo;
    private readonly IReadOnlyRepository<RolePermission> _rolePermissionRoRepo;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IMemoryCache _memoryCache;
    private readonly PermissionSettings _permissionSettings;
    private readonly ILogger<PermissionService> _logger;

    // The service is scoped, so these live for one request only.
    private readonly Dictionary<Guid, HashSet<string>> _permissionsPerCompany = new();
    private readonly HashSet<string> _loggedDenials = new(StringComparer.Ordinal);

    public PermissionService(IAspCurrentUserService aspCurrentUserService,
        IHttpContextAccessor httpContextAccessor,
        IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo,
        IReadOnlyRepository<Company> companyRoRepo,
        IReadOnlyRepository<RolePermission> rolePermissionRoRepo,
        IPermissionCatalog permissionCatalog,
        IMemoryCache memoryCache,
        PermissionSettings permissionSettings,
        ILogger<PermissionService> logger)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _httpContextAccessor = httpContextAccessor;
        _usersInCompanyRoRepo = usersInCompanyRoRepo;
        _companyRoRepo = companyRoRepo;
        _rolePermissionRoRepo = rolePermissionRoRepo;
        _permissionCatalog = permissionCatalog;
        _memoryCache = memoryCache;
        _permissionSettings = permissionSettings;
        _logger = logger;
    }

    public static string GetRoleCacheKey(Guid roleId) => $"ITBees.UserManager.RolePermissions.{roleId}";

    public IReadOnlyCollection<string> GetMyPermissions(Guid companyGuid)
    {
        return GetPermissions(companyGuid).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    public bool HasPermission(string permissionKey, Guid companyGuid)
    {
        return GetPermissions(companyGuid).Contains(permissionKey);
    }

    public void RequirePermission(string permissionKey, Guid companyGuid)
    {
        if (HasPermission(permissionKey, companyGuid))
            return;

        LogDenied(new[] { permissionKey }, companyGuid, enforced: true);
        throw new Authorization403ForbiddenException("You don't have permission to perform this operation.");
    }

    public void CheckEndpointPermission(Guid companyGuid)
    {
        var permissionKeys = GetEndpointPermissionKeys();
        if (permissionKeys.Count == 0)
            return;

        var permissions = GetPermissions(companyGuid);
        if (permissionKeys.Any(permissions.Contains))
            return;

        LogDenied(permissionKeys, companyGuid, _permissionSettings.EnforceEndpointPermissions);
        if (_permissionSettings.EnforceEndpointPermissions)
            throw new Authorization403ForbiddenException("You don't have permission to perform this operation.");
    }

    public bool CanEnterEndpoint(IReadOnlyCollection<string> permissionKeys)
    {
        var userGuid = _aspCurrentUserService.GetCurrentUserGuid();
        if (userGuid == null || permissionKeys.Count == 0 || _aspCurrentUserService.CurrentUserIsPlatformOperator())
            return true;

        var companyGuids = _usersInCompanyRoRepo.GetData(x => x.UserAccountGuid == userGuid)
            .Select(x => x.CompanyGuid)
            .Distinct()
            .ToList();

        if (companyGuids.Any(companyGuid => permissionKeys.Any(GetPermissions(companyGuid).Contains)))
            return true;

        LogDenied(permissionKeys, null, _permissionSettings.EnforceEndpointPermissions);
        return _permissionSettings.EnforceEndpointPermissions == false;
    }

    private HashSet<string> GetPermissions(Guid companyGuid)
    {
        if (_permissionsPerCompany.TryGetValue(companyGuid, out var permissions))
            return permissions;

        permissions = ResolvePermissions(companyGuid);
        _permissionsPerCompany[companyGuid] = permissions;
        return permissions;
    }

    private HashSet<string> ResolvePermissions(Guid companyGuid)
    {
        var userGuid = _aspCurrentUserService.GetCurrentUserGuid();
        if (userGuid == null)
            return NoPermissions;

        if (_aspCurrentUserService.CurrentUserIsPlatformOperator())
            return new HashSet<string>(_permissionCatalog.AllKeys, StringComparer.Ordinal);

        var memberships = _usersInCompanyRoRepo
            .GetData(x => x.UserAccountGuid == userGuid && x.CompanyGuid == companyGuid, x => x.IdentityRole)
            .ToList();
        if (memberships.Count == 0)
            return NoPermissions;

        var company = _companyRoRepo.GetFirst(x => x.Guid == companyGuid);
        if (company != null && company.OwnerGuid == userGuid)
            return new HashSet<string>(_permissionCatalog.AllKeys, StringComparer.Ordinal);

        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in memberships.Where(x => x.IdentityRole != null).Select(x => x.IdentityRole))
        {
            permissions.UnionWith(GetRolePermissions(role));
        }

        return permissions;
    }

    private HashSet<string> GetRolePermissions(FasIdentityRole role)
    {
        // The platform operator role cannot be restricted, also when it is held as a company role.
        if (string.Equals(role.Name, Role.PlatformOperator, StringComparison.OrdinalIgnoreCase))
            return new HashSet<string>(_permissionCatalog.AllKeys, StringComparer.Ordinal);

        return _memoryCache.GetOrCreate(GetRoleCacheKey(role.Id), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = RolePermissionsCacheTime;

            var storedKeys = _rolePermissionRoRepo.GetDataQueryable(x => x.RoleId == role.Id)
                .AsNoTracking()
                .Select(x => x.PermissionKey)
                .ToList();
            if (storedKeys.Contains(RolePermission.FullAccessKey))
                return new HashSet<string>(_permissionCatalog.AllKeys, StringComparer.Ordinal);

            // Keys removed from the code catalog stop working even if they are still stored.
            return new HashSet<string>(storedKeys.Where(_permissionCatalog.Contains), StringComparer.Ordinal);
        });
    }

    private IReadOnlyCollection<string> GetEndpointPermissionKeys()
    {
        // GetMetadata returns the most specific attribute: an action-level one replaces the controller-level one.
        var attribute = _httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata
            .GetMetadata<HasPermissionAttribute>();
        return attribute?.PermissionKeys ?? Array.Empty<string>();
    }

    private void LogDenied(IReadOnlyCollection<string> permissionKeys, Guid? companyGuid, bool enforced)
    {
        // Several company checks of one request report the same denial once.
        if (_loggedDenials.Add($"{companyGuid}|{string.Join(",", permissionKeys)}") == false)
            return;

        var request = _httpContextAccessor.HttpContext?.Request;
        _logger.LogWarning(
            "Permission denied{Mode}: user {UserGuid}, company {CompanyGuid}, required any of [{PermissionKeys}], endpoint {Method} {Path}",
            enforced ? string.Empty : " (shadow mode - request allowed)",
            _aspCurrentUserService.GetCurrentUserGuid(),
            companyGuid?.ToString() ?? "any",
            string.Join(", ", permissionKeys),
            request?.Method,
            request?.Path.Value);
    }
}
