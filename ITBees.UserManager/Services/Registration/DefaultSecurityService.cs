using System;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services.Acl;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Registration;

public class DefaultSecurityService<T, TCompany> : IExternalSecurityService  where T : IdentityUser<Guid>, new() where TCompany : Company, new()
{
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IAccessControlService _accessControlService;

    public DefaultSecurityService(IAspCurrentUserService aspCurrentUserService, IAccessControlService accessControlService)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _accessControlService = accessControlService;
    }
    public CurrentUser GetCurrentUser()
    {
        return _aspCurrentUserService.GetCurrentUser();
    }

    public AccessControlResult CheckUserAccessToMethod(CurrentUser currentUser, Type type,
        string methodNameForAccessCheck, Guid companyGuid)
    {
        return _accessControlService.CanDo(currentUser, typeof(NewUserRegistrationService<T, TCompany>),
            methodNameForAccessCheck, companyGuid); 
    }
}