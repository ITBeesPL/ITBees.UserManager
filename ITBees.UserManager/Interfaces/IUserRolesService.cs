using System;
using System.Collections.Generic;
using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces;

public interface IUserRolesService
{
    List<UserRoleVm> Get();
    UserRoleVm Create(string roleName);
    UserRoleVm GetRole(Guid roleGuid);
    void Delete(Guid roleGuid);
}