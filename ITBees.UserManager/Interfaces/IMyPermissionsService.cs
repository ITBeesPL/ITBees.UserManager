using System;
using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces;

public interface IMyPermissionsService
{
    /// <summary>Permissions of the current user in the company (the last used company when null).</summary>
    MyPermissionsVm Get(Guid? companyGuid);
}
