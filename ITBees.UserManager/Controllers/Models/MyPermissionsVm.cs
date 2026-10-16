using System;
using System.Collections.Generic;
using System.Linq;

namespace ITBees.UserManager.Controllers.Models;

public class MyPermissionsVm
{
    public MyPermissionsVm()
    {
    }

    public MyPermissionsVm(Guid companyGuid, IEnumerable<string> permissionKeys)
    {
        CompanyGuid = companyGuid;
        PermissionKeys = permissionKeys.ToList();
    }

    public Guid CompanyGuid { get; set; }
    public List<string> PermissionKeys { get; set; }
}
