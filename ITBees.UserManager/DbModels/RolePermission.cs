using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.DbModels;

/// <summary>
/// One permission granted to a role, managed in the admin panel. A role without rows holds no permissions.
/// </summary>
public class RolePermission
{
    /// <summary>Stored instead of the keys for a role that holds every permission, including future ones.</summary>
    public const string FullAccessKey = "*";

    public Guid RoleId { get; set; }
    public FasIdentityRole Role { get; set; }
    public string PermissionKey { get; set; }
}
