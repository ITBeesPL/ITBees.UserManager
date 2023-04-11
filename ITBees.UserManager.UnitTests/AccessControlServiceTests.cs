using System;
using System.Collections.Generic;
using ITBees.Models.Users;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.Registration;
using Microsoft.AspNetCore.Identity;
using NUnit.Framework;

namespace ITBees.UserManager.UnitTests
{
    public class AccessControlServiceTests
    {
        [Test]
        public void Test()
        {
            var user = new CurrentUser() {Guid= Guid.NewGuid()};
            var accessControl = new AccessControlService();
            accessControl.Setup(new List<Type>(){typeof(NewUserRegistrationService<>)});
            Guid companyGuid = new Guid();
            
            var accessControlResult = accessControl.CanDo(user, typeof(NewUserRegistrationService<>), nameof(NewUserRegistrationService<IdentityUser>.CreateAndInviteNewUserToCompany), companyGuid);

            Assert.True(accessControlResult.CanDoResult);
        }
    }
}
