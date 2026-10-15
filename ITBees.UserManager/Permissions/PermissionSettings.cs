namespace ITBees.UserManager.Permissions;

public class PermissionSettings
{
    /// <summary>Configuration key (bool) that turns the enforcement of [HasPermission] on.</summary>
    public const string EnforceEndpointPermissionsConfigKey = "PermissionsEnforceEndpoints";

    public PermissionSettings(bool enforceEndpointPermissions)
    {
        EnforceEndpointPermissions = enforceEndpointPermissions;
    }

    /// <summary>
    /// false = shadow mode: [HasPermission] is evaluated and every denial is logged, but the request
    /// is let through. Explicit <see cref="IPermissionService.RequirePermission"/> calls are always enforced.
    /// </summary>
    public bool EnforceEndpointPermissions { get; }
}
