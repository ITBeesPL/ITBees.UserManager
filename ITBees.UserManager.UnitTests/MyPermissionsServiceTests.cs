using System;
using System.Collections.Generic;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Permissions;
using ITBees.UserManager.Services.Permissions;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    public class MyPermissionsServiceTests
    {
        private const string OpenBarrier = "test.barrier.open";

        private readonly Guid _userGuid = Guid.NewGuid();
        private readonly Guid _companyGuid = Guid.NewGuid();
        private readonly FasIdentityRole _gateOpener = new("GateOpener") { Id = Guid.NewGuid() };

        private MyPermissionsService CreateService(CurrentUser currentUser)
        {
            var memberships = new FakeRepository<UsersInCompany>(new UsersInCompany
            {
                UserAccountGuid = _userGuid, CompanyGuid = _companyGuid,
                IdentityRole = _gateOpener, IdentityRoleId = _gateOpener.Id
            });
            var permissionService = new PermissionService(
                new FakeAspCurrentUserService(currentUser),
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                memberships,
                new FakeRepository<Company>(new Company { Guid = _companyGuid, OwnerGuid = Guid.NewGuid() }),
                new FakeRepository<RolePermission>(new RolePermission
                {
                    RoleId = _gateOpener.Id, PermissionKey = OpenBarrier
                }),
                new PermissionCatalog(new[] { new TestCatalog() }),
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<PermissionService>.Instance);

            return new MyPermissionsService(new FakeAspCurrentUserService(currentUser), permissionService);
        }

        [Test]
        public void Without_a_company_the_last_used_one_is_answered()
        {
            var currentUser = new CurrentUser { Guid = _userGuid, LastUsedCompanyGuid = _companyGuid };

            var result = CreateService(currentUser).Get(null);

            Assert.That(result.CompanyGuid, Is.EqualTo(_companyGuid));
            Assert.That(result.PermissionKeys, Is.EquivalentTo(new[] { OpenBarrier }));
        }

        [Test]
        public void A_user_without_a_company_and_without_a_last_used_one_gets_a_bad_request()
        {
            var currentUser = new CurrentUser { Guid = _userGuid, LastUsedCompanyGuid = Guid.Empty };

            Assert.Throws<FasApiErrorException>(() => CreateService(currentUser).Get(null));
        }

        [Test]
        public void An_anonymous_request_is_refused()
        {
            Assert.Throws<AuthorizationException>(() => CreateService(null).Get(_companyGuid));
        }

        private class TestCatalog : IPermissionCatalogContributor
        {
            public IEnumerable<string> GetPermissionKeys() => new[] { OpenBarrier };
        }
    }
}
