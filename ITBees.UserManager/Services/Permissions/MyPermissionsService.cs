using System;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.RestfulApiControllers.Models;
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

        // A company the user does not belong to simply yields no permissions, but an empty guid is a bad request
        // rather than a user without permissions - the caller sent nothing and has no last used company either.
        var targetCompanyGuid = companyGuid ?? currentUser.LastUsedCompanyGuid;
        if (targetCompanyGuid == Guid.Empty)
            throw new FasApiErrorException(new FasApiErrorVm(
                "No company given and the user has no last used company", 400, ""));

        return new MyPermissionsVm(targetCompanyGuid, _permissionService.GetMyPermissions(targetCompanyGuid));
    }
}
