using System.Collections.Generic;

namespace ITBees.UserManager.Permissions;

/// <summary>
/// Registered (as a singleton) by every application or library that checks permissions. Only the keys live in code -
/// they are referenced by [HasPermission] and by the panels. Names, groups and the roles holding a key are data
/// (tables Permission and RolePermission): seeded by a migration and managed in the admin panel.
/// </summary>
public interface IPermissionCatalogContributor
{
    /// <summary>Keys in the form "app.area.action"; a key that is in use is never renamed.</summary>
    IEnumerable<string> GetPermissionKeys();
}
