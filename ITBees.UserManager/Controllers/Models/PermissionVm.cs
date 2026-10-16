using System.Collections.Generic;
using System.Linq;
using ITBees.UserManager.DbModels;

namespace ITBees.UserManager.Controllers.Models;

public class PermissionVm
{
    public const string UndescribedGroupName = "Other";

    public PermissionVm()
    {
    }

    public PermissionVm(Permission x, IEnumerable<string> roleNames)
    {
        Key = x.Key;
        Name = x.Name;
        GroupName = x.GroupName;
        Description = x.Description;
        RoleNames = roleNames.ToList();
    }

    /// <summary>A key declared in code that has no row in the database yet (no migration seeded it).</summary>
    public PermissionVm(string key, IEnumerable<string> roleNames)
    {
        Key = key;
        Name = key;
        GroupName = UndescribedGroupName;
        RoleNames = roleNames.ToList();
    }

    public string Key { get; set; }
    public string Name { get; set; }
    public string GroupName { get; set; }
    public string Description { get; set; }

    /// <summary>Roles granted this permission explicitly (full-access roles are listed in the catalog).</summary>
    public List<string> RoleNames { get; set; }
}
