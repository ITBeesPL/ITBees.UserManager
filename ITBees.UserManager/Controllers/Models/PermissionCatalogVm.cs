using System.Collections.Generic;
using System.Linq;

namespace ITBees.UserManager.Controllers.Models;

public class PermissionCatalogVm
{
    public PermissionCatalogVm()
    {
    }

    /// <param name="permissions">Permissions in display order; groups keep the order of their first permission.</param>
    /// <param name="fullAccessRoleNames">Roles holding every permission.</param>
    public PermissionCatalogVm(IEnumerable<PermissionVm> permissions, IEnumerable<string> fullAccessRoleNames)
    {
        Groups = permissions
            .GroupBy(x => x.GroupName)
            .Select(x => new PermissionGroupVm(x.Key, x))
            .ToList();
        FullAccessRoleNames = fullAccessRoleNames.ToList();
    }

    public List<PermissionGroupVm> Groups { get; set; }
    public List<string> FullAccessRoleNames { get; set; }
}
