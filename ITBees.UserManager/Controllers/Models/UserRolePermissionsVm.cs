using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Models.Users;

namespace ITBees.UserManager.Controllers.Models;

public class UserRolePermissionsVm
{
    public UserRolePermissionsVm()
    {
    }

    public UserRolePermissionsVm(FasIdentityRole role, bool fullAccess, IEnumerable<string> permissionKeys)
    {
        RoleGuid = role.Id;
        RoleName = role.Name;
        FullAccess = fullAccess;
        PermissionKeys = permissionKeys.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    public Guid RoleGuid { get; set; }
    public string RoleName { get; set; }

    /// <summary>The role holds every permission, including the ones added in the future.</summary>
    public bool FullAccess { get; set; }

    /// <summary>Permissions of the role when it has no full access.</summary>
    public List<string> PermissionKeys { get; set; }
}
