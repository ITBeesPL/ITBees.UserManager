using System;
using System.Collections.Generic;
using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces;

public interface IUserRolesService
{
    List<UserRoleVm> Get();
    UserRoleVm Create(string roleName);
    UserRoleVm GetRole(Guid roleGuid);

    /// <summary>
    /// Deletes the role. When users hold it (in a company, an invitation or as a global role), replacementRoleGuid is
    /// required and they get that role instead.
    /// </summary>
    void Delete(Guid roleGuid, Guid? replacementRoleGuid);
}