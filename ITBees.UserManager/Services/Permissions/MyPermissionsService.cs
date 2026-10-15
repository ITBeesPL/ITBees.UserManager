using System;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Permissions;

namespace ITBees.UserManager.Services.Permissions;

public class MyPermissionsService : IMyPermissionsService
{
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IPermissionService _permissionService;

    public MyPermissionsService(IAspCurrentUserService aspCurrentUserService, IPermissionService permissionService)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _permissionService = permissionService;
    }

    public MyPermissionsVm Get(Guid? companyGuid)
    {
        var currentUser = _aspCurrentUserService.GetCurrentUser();
        if (currentUser == null)
            throw new AuthorizationException(AuthorizationExceptionMessages.You_dont_have_any_acceess_to_company);

        // A company the user does not belong to simply yields no permissions.
        var targetCompanyGuid = companyGuid ?? currentUser.LastUsedCompanyGuid;
        return new MyPermissionsVm(targetCompanyGuid, _permissionService.GetMyPermissions(targetCompanyGuid));
    }
}
