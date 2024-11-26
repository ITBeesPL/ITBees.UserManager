using System;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Controllers.Models;

public class UserRoleVm
{
    public UserRoleVm()
    {
        
    }

    public UserRoleVm(IdentityRole x)
    {
        RoleName = x.Name;
        Guid = System.Guid.Parse(x.Id);
    }

    public Guid Guid { get; set; }
    public string RoleName { get; set; }
}