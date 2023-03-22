using System;
using System.Collections.Generic;
using System.Diagnostics;
using ITBees.Models.Users;

namespace ITBees.UserManager.Services.Acl
{
    public class AccessControlService : IAccessControlService
    {
        public void Setup(List<Type> types)
        {
            
        }

        public AccessControlResult CanDo(CurrentUser getCurrentUser, Type type, string methodName, Guid companyGuid)
        {
            var rememberToImplementLogicForAclChecking = "Remember to implement logic for ACL checking !!!";
            Debug.WriteLine(rememberToImplementLogicForAclChecking);
            Console.WriteLine(rememberToImplementLogicForAclChecking);
            //Console.Beep();
            return new AccessControlResult(true, string.Empty) ;
        }
    }
}