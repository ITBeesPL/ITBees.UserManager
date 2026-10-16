using System.Collections.Generic;
using System.Linq;

namespace ITBees.UserManager.Controllers.Models;

public class PermissionGroupVm
{
    public PermissionGroupVm()
    {
    }

    public PermissionGroupVm(string name, IEnumerable<PermissionVm> permissions)
    {
        Name = name;
        Permissions = permissions.ToList();
    }

    public string Name { get; set; }
    public List<PermissionVm> Permissions { get; set; }
}
