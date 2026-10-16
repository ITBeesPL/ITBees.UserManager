using System;

namespace ITBees.UserManager.DbModels;

/// <summary>
/// How a permission key declared in code (IPermissionCatalogContributor) is shown: seeded by a migration and edited in
/// the admin panel. A row whose key is no longer declared in code is ignored.
/// </summary>
public class Permission
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string GroupName { get; set; }
    public string Description { get; set; }

    /// <summary>Orders the permissions in the role editor.</summary>
    public int SortOrder { get; set; }

    /// <summary>Initialised here, so a row inserted without it does not land on a date MySQL refuses.</summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
