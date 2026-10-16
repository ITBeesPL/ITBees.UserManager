using System;
using System.Collections.Generic;
using ITBees.Models.Users;

namespace ITBees.UserManager.Permissions;

/// <summary>
/// Permissions of the current user, always resolved on the server for a given company (never taken from the token).
/// </summary>
public interface IPermissionService
{
    /// <summary>Effective permission keys of the current user in the company; empty when the user is not a member.</summary>
    IReadOnlyCollection<string> GetMyPermissions(Guid companyGuid);

    bool HasPermission(string permissionKey, Guid companyGuid);

    /// <summary>Throws Authorization403ForbiddenException (and logs) when the user lacks the permission in the company.</summary>
    void RequirePermission(string permissionKey, Guid companyGuid);

    /// <summary>
    /// Checks the [HasPermission] of the current endpoint against the company. Called by every company access
    /// check (IAspCurrentUserService.TryCanIDoForCompany). Does nothing for endpoints without [HasPermission].
    /// </summary>
    void CheckEndpointPermission(Guid companyGuid);

    /// <summary>
    /// Like [HasPermission], for a permission that depends on the request (e.g. a filter that also lists closed
    /// sessions): the user needs any of the keys in the company, otherwise Authorization403ForbiddenException.
    /// </summary>
    void CheckAnyPermission(Guid companyGuid, params string[] permissionKeys);

    /// <summary>
    /// Used by [HasPermission] before the action runs: true when the user holds any of the keys in at least one
    /// of their companies.
    /// </summary>
    bool CanEnterEndpoint(IReadOnlyCollection<string> permissionKeys);

    /// <summary>
    /// True when the current user may give the role to someone in the company: the role holds nothing the user does
    /// not hold there. A full-access role needs full access, the platform operator role a platform operator.
    /// Use it to filter role pickers.
    /// </summary>
    bool CanGrantRole(FasIdentityRole role, Guid companyGuid);

    /// <summary>
    /// Call before a role is granted (an invitation, a membership change): Authorization403ForbiddenException (and a
    /// log entry) when <see cref="CanGrantRole"/> is false.
    /// </summary>
    void CheckRoleGrant(FasIdentityRole role, Guid companyGuid);
}
