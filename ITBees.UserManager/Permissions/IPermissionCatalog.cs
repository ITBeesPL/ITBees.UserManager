using System.Collections.Generic;

namespace ITBees.UserManager.Permissions;

/// <summary>Permission keys declared in code by all registered contributors.</summary>
public interface IPermissionCatalog
{
    /// <summary>All keys in the order the contributors declared them.</summary>
    IReadOnlyList<string> AllKeys { get; }

    bool Contains(string permissionKey);
}
