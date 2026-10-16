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
    private static readonly Grant NoGrant = new(false, new HashSet<string>(StringComparer.Ordinal));

    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
    private readonly IReadOnlyRepository<Company> _companyRoRepo;
    private readonly IReadOnlyRepository<RolePermission> _rolePermissionRoRepo;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PermissionService> _logger;

    // The service is scoped, so these live for one request only.
    private readonly Dictionary<Guid, Grant> _grantsPerCompany = new();
    private readonly HashSet<string> _loggedDenials = new(StringComparer.Ordinal);

    public PermissionService(IAspCurrentUserService aspCurrentUserService,
        IHttpContextAccessor httpContextAccessor,
        IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo,
        IReadOnlyRepository<Company> companyRoRepo,
        IReadOnlyRepository<RolePermission> rolePermissionRoRepo,
        IPermissionCatalog permissionCatalog,
        IMemoryCache memoryCache,
        ILogger<PermissionService> logger)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _httpContextAccessor = httpContextAccessor;
        _usersInCompanyRoRepo = usersInCompanyRoRepo;
        _companyRoRepo = companyRoRepo;
        _rolePermissionRoRepo = rolePermissionRoRepo;
        _permissionCatalog = permissionCatalog;
        _memoryCache = memoryCache;
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
        CheckAny(new[] { permissionKey }, companyGuid);
    }

    public void CheckEndpointPermission(Guid companyGuid)
    {
        CheckAny(GetEndpointPermissionKeys(), companyGuid);
    }

    public void CheckAnyPermission(Guid companyGuid, params string[] permissionKeys)
    {
        CheckAny(permissionKeys ?? Array.Empty<string>(), companyGuid);
    }

    private void CheckAny(IReadOnlyCollection<string> permissionKeys, Guid companyGuid)
    {
        if (permissionKeys.Count == 0)
            return;

        var permissions = GetPermissions(companyGuid);
        if (permissionKeys.Any(permissions.Contains))
            return;

        LogDenied(permissionKeys, companyGuid);
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

        LogDenied(permissionKeys, null);
        return false;
    }

    public bool CanGrantRole(FasIdentityRole role, Guid companyGuid)
    {
        if (role == null)
            return false;

        if (_aspCurrentUserService.CurrentUserIsPlatformOperator())
            return true;

        if (IsPlatformOperatorRole(role))
            return false;

        var granter = GetGrant(companyGuid);
        var granted = GetRoleGrant(role);

        // A full-access role also holds every key added later, so only someone with full access may hand it out.
        return granted.FullAccess ? granter.FullAccess : granted.Keys.IsSubsetOf(granter.Keys);
    }

    public void CheckRoleGrant(FasIdentityRole role, Guid companyGuid)
    {
        if (CanGrantRole(role, companyGuid))
            return;

        _logger.LogWarning(
            "Role grant denied: user {UserGuid}, company {CompanyGuid}, role {RoleName} holds permissions the user does not hold",
            _aspCurrentUserService.GetCurrentUserGuid(),
            companyGuid,
            role?.Name);

        throw new Authorization403ForbiddenException("You cannot grant a role with permissions you do not hold.");
    }

    private HashSet<string> GetPermissions(Guid companyGuid) => GetGrant(companyGuid).Keys;

    private Grant GetGrant(Guid companyGuid)
    {
        if (_grantsPerCompany.TryGetValue(companyGuid, out var grant))
            return grant;

        grant = ResolveGrant(companyGuid);
        _grantsPerCompany[companyGuid] = grant;
        return grant;
    }

    private Grant ResolveGrant(Guid companyGuid)
    {
        var userGuid = _aspCurrentUserService.GetCurrentUserGuid();
        if (userGuid == null)
            return NoGrant;

        if (_aspCurrentUserService.CurrentUserIsPlatformOperator())
            return FullAccessGrant();

        var memberships = _usersInCompanyRoRepo
            .GetData(x => x.UserAccountGuid == userGuid && x.CompanyGuid == companyGuid, x => x.IdentityRole)
            .ToList();
        if (memberships.Count == 0)
            return NoGrant;

        var company = _companyRoRepo.GetFirst(x => x.Guid == companyGuid);
        if (company != null && company.OwnerGuid == userGuid)
            return FullAccessGrant();

        var fullAccess = false;
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in memberships.Where(x => x.IdentityRole != null).Select(x => x.IdentityRole))
        {
            var roleGrant = GetRoleGrant(role);
            fullAccess |= roleGrant.FullAccess;
            permissions.UnionWith(roleGrant.Keys);
        }

        return new Grant(fullAccess, permissions);
    }

    private Grant GetRoleGrant(FasIdentityRole role)
    {
        // The platform operator role cannot be restricted, also when it is held as a company role.
        if (IsPlatformOperatorRole(role))
            return FullAccessGrant();

        return _memoryCache.GetOrCreate(GetRoleCacheKey(role.Id), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = RolePermissionsCacheTime;

            var storedKeys = _rolePermissionRoRepo.GetDataQueryable(x => x.RoleId == role.Id)
                .AsNoTracking()
                .Select(x => x.PermissionKey)
                .ToList();
            if (storedKeys.Contains(RolePermission.FullAccessKey))
                return FullAccessGrant();

            // Keys removed from the code catalog stop working even if they are still stored.
            return new Grant(false,
                new HashSet<string>(storedKeys.Where(_permissionCatalog.Contains), StringComparer.Ordinal));
        });
    }

    private Grant FullAccessGrant() =>
        new(true, new HashSet<string>(_permissionCatalog.AllKeys, StringComparer.Ordinal));

    private static bool IsPlatformOperatorRole(FasIdentityRole role) =>
        string.Equals(role.Name, Role.PlatformOperator, StringComparison.OrdinalIgnoreCase);

    /// <summary>Keys held in a company or by a role; FullAccess also covers the keys added in the future.</summary>
    private sealed record Grant(bool FullAccess, HashSet<string> Keys);

    private IReadOnlyCollection<string> GetEndpointPermissionKeys()
    {
        // GetMetadata returns the most specific attribute: an action-level one replaces the controller-level one.
        var attribute = _httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata
            .GetMetadata<HasPermissionAttribute>();
        return attribute?.PermissionKeys ?? Array.Empty<string>();
    }

    private void LogDenied(IReadOnlyCollection<string> permissionKeys, Guid? companyGuid)
    {
        // Several company checks of one request report the same denial once.
        if (_loggedDenials.Add($"{companyGuid}|{string.Join(",", permissionKeys)}") == false)
            return;

        var request = _httpContextAccessor.HttpContext?.Request;
        _logger.LogWarning(
            "Permission denied: user {UserGuid}, company {CompanyGuid}, required any of [{PermissionKeys}], endpoint {Method} {Path}",
            _aspCurrentUserService.GetCurrentUserGuid(),
            companyGuid?.ToString() ?? "any",
            string.Join(", ", permissionKeys),
            request?.Method,
            request?.Path.Value);
    }
}
