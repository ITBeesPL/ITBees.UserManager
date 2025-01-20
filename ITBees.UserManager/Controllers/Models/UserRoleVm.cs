using System;
using ITBees.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Controllers.Models;

public class UserRoleVm
{
    public UserRoleVm()
    {
        
    }

    public UserRoleVm(FasIdentityRole x)
    {
        RoleName = x.Name;
        Guid = x.Id;
        RoleDescription = x.RoleDescription;
    }

    public Guid Guid { get; set; }
    public string RoleName { get; set; }
    public string RoleDescription { get; set; }
}