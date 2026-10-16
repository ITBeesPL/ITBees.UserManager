using System;
using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces;

public interface IUserRolePermissionsService
{
    UserRolePermissionsVm Get(Guid roleGuid);
    UserRolePermissionsVm Update(UserRolePermissionsUm userRolePermissionsUm);
}
