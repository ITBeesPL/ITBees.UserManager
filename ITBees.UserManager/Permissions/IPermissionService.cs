using System;
using System.Collections.Generic;

namespace ITBees.UserManager.Permissions;

/// <summary>
/// Permissions of the current user, always resolved on the server for a given company (never taken from the token).
/// </summary>
public interface IPermissionService
{
    /// <summary>Effective permission keys of the current user in the company; empty when the user is not a member.</summary>
    IReadOnlyCollection<string> GetMyPermissions(Guid companyGuid);

    bool HasPermission(string permissionKey, Guid companyGuid);

    /// <summary>Always enforced (also in shadow mode). Throws Authorization403ForbiddenException.</summary>
    void RequirePermission(string permissionKey, Guid companyGuid);

    /// <summary>
    /// Checks the [HasPermission] of the current endpoint against the company. Called by every company access
    /// check (IAspCurrentUserService.TryCanIDoForCompany). Does nothing for endpoints without [HasPermission].
    /// </summary>
    void CheckEndpointPermission(Guid companyGuid);

    /// <summary>
    /// Used by [HasPermission] before the action runs: true when the user holds any of the keys in at least one
    /// of their companies, or when the denial is only logged (shadow mode).
    /// </summary>
    bool CanEnterEndpoint(IReadOnlyCollection<string> permissionKeys);
}
