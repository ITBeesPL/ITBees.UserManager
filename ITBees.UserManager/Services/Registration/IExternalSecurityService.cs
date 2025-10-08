using System;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.UserManager.Services.Acl;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Registration;

public interface IExternalSecurityService
{
    CurrentUser GetCurrentUser();
    AccessControlResult CheckUserAccessToMethod(CurrentUser currentUser, Type type, string methodNameForAccessCheck, Guid companyGuid);
}