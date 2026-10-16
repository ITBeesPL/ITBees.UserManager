using System;
using System.Collections.Generic;

namespace ITBees.UserManager.Controllers.Models;

public class UserRolePermissionsUm
{
    public Guid RoleGuid { get; set; }

    /// <summary>True = every permission, including future ones; PermissionKeys are then ignored.</summary>
    public bool FullAccess { get; set; }

    public List<string> PermissionKeys { get; set; }
}
