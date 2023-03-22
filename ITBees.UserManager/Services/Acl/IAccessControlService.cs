using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.Services.Acl
{
    public interface IAccessControlService
    {
        AccessControlResult CanDo(CurrentUser getCurrentUser, Type type, string methodName, Guid companyGuid);
    }
}