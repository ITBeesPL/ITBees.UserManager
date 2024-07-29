using System;
using System.Collections.Generic;
using ITBees.Models.Companies;
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
            accessControl.Setup(new List<Type>(){typeof(NewUserRegistrationService<IdentityUser,Company>)});
            Guid companyGuid = new Guid();
            
            var accessControlResult = accessControl.CanDo(user, typeof(NewUserRegistrationService<IdentityUser, Company>), nameof(NewUserRegistrationService<IdentityUser, Company>.CreateAndInviteNewUserToCompany), companyGuid);

            Assert.That(accessControlResult.CanDoResult);
        }
    }
}
