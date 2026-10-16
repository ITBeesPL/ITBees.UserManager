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
    private readonly HashSet<string> _loggedUnknownKeys = new(StringComparer.Ordinal);

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
        return GetGrant(companyGuid).Allows(permissionKey);
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
        // An empty list would silently allow everything - a caller asking for "any of nothing" is a mistake.
        if (permissionKeys == null || permissionKeys.Length == 0)
            throw new ArgumentException("At least one permission key is required.", nameof(permissionKeys));

        CheckAny(permissionKeys, companyGuid);
    }

    private void CheckAny(IReadOnlyCollection<string> permissionKeys, Guid companyGuid)
    {
        // Endpoints without [HasPermission] need only the membership, so an empty list passes here.
        if (permissionKeys.Count == 0)
            return;

        WarnAboutKeysOutsideTheCatalog(permissionKeys);

        var grant = GetGrant(companyGuid);
        if (permissionKeys.Any(grant.Allows))
            return;

        LogDenied(permissionKeys, companyGuid);
        throw new Authorization403ForbiddenException("You don't have permission to perform this operation.");
    }

    public bool CanEnterEndpoint(IReadOnlyCollection<string> permissionKeys)
    {
        if (permissionKeys.Count == 0)
            return true;

        if (_aspCurrentUserService.CurrentUserIsPlatformOperator())
            return true;

        var userGuid = _aspCurrentUserService.GetCurrentUserGuid();
        if (userGuid == null)
            return false;

        WarnAboutKeysOutsideTheCatalog(permissionKeys);

        // One query for every company of the user; the role grants behind it are cached per role.
        var memberships = _usersInCompanyRoRepo
            .GetData(x => x.UserAccountGuid == userGuid, x => x.IdentityRole)
            .ToList();

        var roleHoldsIt = memberships
            .Select(x => x.IdentityRole)
            .Where(x => x != null)
            .DistinctBy(x => x.Id)
            .Any(role => permissionKeys.Any(GetRoleGrant(role).Allows));
        if (roleHoldsIt)
            return true;

        // The owner of a company holds every permission in it, with or without a role.
        var companyGuids = memberships.Select(x => x.CompanyGuid).Distinct().ToList();
        if (_companyRoRepo.HasData(x => companyGuids.Contains(x.Guid) && x.OwnerGuid == userGuid))
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
    private sealed record Grant(bool FullAccess, HashSet<string> Keys)
    {
        /// <summary>
        /// Full access holds a key even when the catalog does not know it yet (or any more), so a key that no
        /// contributor declares does not lock out the platform operator, the company owner and full-access roles.
        /// </summary>
        public bool Allows(string permissionKey) => FullAccess || Keys.Contains(permissionKey);
    }

    private IReadOnlyCollection<string> GetEndpointPermissionKeys()
    {
        // GetMetadata returns the most specific attribute: an action-level one replaces the controller-level one.
        var attribute = _httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata
            .GetMetadata<HasPermissionAttribute>();
        return attribute?.PermissionKeys ?? Array.Empty<string>();
    }

    /// <summary>
    /// A required key that no contributor declares can only ever be held through full access, so a typo in
    /// [HasPermission] or a contributor that is not registered would quietly refuse every ordinary role. Reported
    /// as an error instead, once per request.
    /// </summary>
    private void WarnAboutKeysOutsideTheCatalog(IReadOnlyCollection<string> permissionKeys)
    {
        var unknownKeys = permissionKeys.Where(x => _permissionCatalog.Contains(x) == false).ToList();
        if (unknownKeys.Count == 0 || _loggedUnknownKeys.Add(string.Join(",", unknownKeys)) == false)
            return;

        _logger.LogError(
            "Permission keys [{PermissionKeys}] are required but not declared by any IPermissionCatalogContributor - " +
            "no role can hold them. Check the key and the registration of the contributor.",
            string.Join(", ", unknownKeys));
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
