using System;
using System.Collections.Generic;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Permissions;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// Permissions are resolved per company on the server: owner and platform operator hold everything, a member
    /// holds the permissions of their company role (stored, or the catalog defaults), anyone else holds nothing.
    /// </summary>
    public class PermissionServiceTests
    {
        private const string OpenBarrier = "test.barrier.open";
        private const string EditTariff = "test.tariff.edit";
        private const string ViewPayments = "test.payments.view";

        private readonly Guid _userGuid = Guid.NewGuid();
        private readonly Guid _companyGuid = Guid.NewGuid();
        private readonly Guid _otherCompanyGuid = Guid.NewGuid();
        private readonly FasIdentityRole _gateOpener = new("GateOpener") { Id = Guid.NewGuid() };

        private FakeRepository<UsersInCompany> _memberships;
        private FakeRepository<Company> _companies;
        private FakeRepository<RolePermission> _rolePermissions;
        private DefaultHttpContext _httpContext;

        [SetUp]
        public void SetUp()
        {
            _memberships = new FakeRepository<UsersInCompany>(new UsersInCompany
            {
                UserAccountGuid = _userGuid, CompanyGuid = _companyGuid,
                IdentityRole = _gateOpener, IdentityRoleId = _gateOpener.Id
            });
            _companies = new FakeRepository<Company>(
                new Company { Guid = _companyGuid, OwnerGuid = Guid.NewGuid() },
                new Company { Guid = _otherCompanyGuid, OwnerGuid = Guid.NewGuid() });
            _rolePermissions = new FakeRepository<RolePermission>(
                new RolePermission { RoleId = _gateOpener.Id, PermissionKey = OpenBarrier });
            _httpContext = new DefaultHttpContext();
        }

        private PermissionService CreateService(bool isPlatformOperator = false)
        {
            var catalog = new PermissionCatalog(new[] { new TestCatalog() });
            var currentUser = new CurrentUser { Guid = _userGuid, LastUsedCompanyGuid = _companyGuid };
            return new PermissionService(new FakeAspCurrentUserService(currentUser, isPlatformOperator),
                new HttpContextAccessor { HttpContext = _httpContext }, _memberships, _companies, _rolePermissions,
                catalog, new MemoryCache(new MemoryCacheOptions()), NullLogger<PermissionService>.Instance);
        }

        private void SetEndpoint(params string[] permissionKeys)
        {
            _httpContext.SetEndpoint(new Endpoint(null,
                new EndpointMetadataCollection(new HasPermissionAttribute(permissionKeys)), "test endpoint"));
        }

        [Test]
        public void A_member_holds_the_permissions_stored_for_the_role()
        {
            var permissions = CreateService().GetMyPermissions(_companyGuid);

            Assert.That(permissions, Is.EquivalentTo(new[] { OpenBarrier }));
        }

        [Test]
        public void A_role_without_stored_permissions_holds_nothing()
        {
            _rolePermissions.Items.Clear();

            Assert.That(CreateService().GetMyPermissions(_companyGuid), Is.Empty);
        }

        [Test]
        public void A_full_access_role_holds_every_permission()
        {
            _rolePermissions.Items.Clear();
            _rolePermissions.Items.Add(new RolePermission
            {
                RoleId = _gateOpener.Id, PermissionKey = RolePermission.FullAccessKey
            });

            Assert.That(CreateService().GetMyPermissions(_companyGuid),
                Is.EquivalentTo(new[] { OpenBarrier, EditTariff, ViewPayments }));
        }

        [Test]
        public void The_platform_operator_held_as_a_company_role_holds_every_permission()
        {
            var platformOperator = new FasIdentityRole("PlatformOperator") { Id = Guid.NewGuid() };
            _memberships.Items[0].IdentityRole = platformOperator;
            _memberships.Items[0].IdentityRoleId = platformOperator.Id;

            Assert.That(CreateService().GetMyPermissions(_companyGuid),
                Is.EquivalentTo(new[] { OpenBarrier, EditTariff, ViewPayments }));
        }

        [Test]
        public void A_stored_key_that_is_no_longer_in_the_catalog_is_ignored()
        {
            _rolePermissions.Items.Clear();
            _rolePermissions.Items.Add(new RolePermission { RoleId = _gateOpener.Id, PermissionKey = "test.removed.key" });
            _rolePermissions.Items.Add(new RolePermission { RoleId = _gateOpener.Id, PermissionKey = ViewPayments });

            Assert.That(CreateService().GetMyPermissions(_companyGuid), Is.EquivalentTo(new[] { ViewPayments }));
        }

        [Test]
        public void The_company_owner_holds_every_permission_even_without_a_role()
        {
            _companies.Items[0].OwnerGuid = _userGuid;
            _memberships.Items[0].IdentityRole = null;
            _memberships.Items[0].IdentityRoleId = null;

            Assert.That(CreateService().GetMyPermissions(_companyGuid),
                Is.EquivalentTo(new[] { OpenBarrier, EditTariff, ViewPayments }));
        }

        [Test]
        public void A_member_without_a_role_holds_nothing()
        {
            _memberships.Items[0].IdentityRole = null;
            _memberships.Items[0].IdentityRoleId = null;

            Assert.That(CreateService().GetMyPermissions(_companyGuid), Is.Empty);
        }

        [Test]
        public void A_role_held_in_one_company_grants_nothing_in_another_company()
        {
            Assert.That(CreateService().GetMyPermissions(_otherCompanyGuid), Is.Empty);
        }

        [Test]
        public void The_platform_operator_holds_every_permission_in_any_company()
        {
            Assert.That(CreateService(isPlatformOperator: true).GetMyPermissions(_otherCompanyGuid),
                Is.EquivalentTo(new[] { OpenBarrier, EditTariff, ViewPayments }));
        }

        [Test]
        public void RequirePermission_refuses_a_permission_missing_in_the_company()
        {
            var service = CreateService();

            Assert.DoesNotThrow(() => service.RequirePermission(OpenBarrier, _companyGuid));
            Assert.Throws<Authorization403ForbiddenException>(() => service.RequirePermission(EditTariff, _companyGuid));
        }

        [Test]
        public void An_endpoint_permission_missing_in_the_company_is_refused()
        {
            SetEndpoint(EditTariff);

            Assert.Throws<Authorization403ForbiddenException>(
                () => CreateService().CheckEndpointPermission(_companyGuid));
        }

        [Test]
        public void An_endpoint_accepts_any_of_its_permissions()
        {
            SetEndpoint(EditTariff, OpenBarrier);

            Assert.DoesNotThrow(() => CreateService().CheckEndpointPermission(_companyGuid));
        }

        [Test]
        public void An_endpoint_without_HasPermission_needs_only_the_membership()
        {
            Assert.DoesNotThrow(() => CreateService().CheckEndpointPermission(_otherCompanyGuid));
        }

        [Test]
        public void CheckAnyPermission_accepts_any_of_the_keys_in_the_company()
        {
            Assert.DoesNotThrow(() => CreateService().CheckAnyPermission(_companyGuid, EditTariff, OpenBarrier));
            Assert.Throws<Authorization403ForbiddenException>(
                () => CreateService().CheckAnyPermission(_companyGuid, EditTariff));
            Assert.Throws<Authorization403ForbiddenException>(
                () => CreateService().CheckAnyPermission(_otherCompanyGuid, OpenBarrier));
        }

        [Test]
        public void CanEnterEndpoint_needs_the_permission_in_at_least_one_company_of_the_user()
        {
            var service = CreateService();

            Assert.That(service.CanEnterEndpoint(new[] { OpenBarrier }), Is.True);
            Assert.That(service.CanEnterEndpoint(new[] { EditTariff }), Is.False);
        }

        [Test]
        public void A_member_may_grant_a_role_holding_only_permissions_the_member_holds()
        {
            var limitedRole = RoleWith(OpenBarrier);

            Assert.That(CreateService().CanGrantRole(limitedRole, _companyGuid), Is.True);
            Assert.That(CreateService().CanGrantRole(_gateOpener, _companyGuid), Is.True);
            Assert.That(CreateService().CanGrantRole(RoleWith(), _companyGuid), Is.True);
        }

        [Test]
        public void A_member_may_not_grant_a_role_holding_more_than_the_member()
        {
            var widerRole = RoleWith(OpenBarrier, EditTariff);

            Assert.That(CreateService().CanGrantRole(widerRole, _companyGuid), Is.False);
            Assert.That(CreateService().CanGrantRole(widerRole, _otherCompanyGuid), Is.False);
        }

        [Test]
        public void A_full_access_role_needs_full_access_also_when_every_current_key_is_held()
        {
            _rolePermissions.Items.Clear();
            foreach (var key in new[] { OpenBarrier, EditTariff, ViewPayments })
                _rolePermissions.Items.Add(new RolePermission { RoleId = _gateOpener.Id, PermissionKey = key });
            var fullAccessRole = RoleWith(RolePermission.FullAccessKey);

            Assert.That(CreateService().CanGrantRole(fullAccessRole, _companyGuid), Is.False);

            _companies.Items[0].OwnerGuid = _userGuid;
            Assert.That(CreateService().CanGrantRole(fullAccessRole, _companyGuid), Is.True);
        }

        [Test]
        public void Only_a_platform_operator_may_grant_the_platform_operator_role()
        {
            _companies.Items[0].OwnerGuid = _userGuid;
            var platformOperator = new FasIdentityRole("PlatformOperator") { Id = Guid.NewGuid() };

            Assert.That(CreateService().CanGrantRole(platformOperator, _companyGuid), Is.False);
            Assert.That(CreateService(isPlatformOperator: true).CanGrantRole(platformOperator, _companyGuid), Is.True);
        }

        [Test]
        public void CheckRoleGrant_refuses_a_role_holding_more_than_the_granter()
        {
            var widerRole = RoleWith(OpenBarrier, EditTariff);

            Assert.Throws<Authorization403ForbiddenException>(
                () => CreateService().CheckRoleGrant(widerRole, _companyGuid));
            Assert.DoesNotThrow(() => CreateService().CheckRoleGrant(_gateOpener, _companyGuid));
        }

        [Test]
        public void A_key_no_contributor_declares_is_still_held_through_full_access()
        {
            // A typo in [HasPermission] or a contributor that is not registered must not lock out the operator too.
            const string undeclared = "test.undeclared.key";

            Assert.That(CreateService(isPlatformOperator: true).HasPermission(undeclared, _companyGuid), Is.True);
            Assert.DoesNotThrow(() => CreateService(isPlatformOperator: true)
                .CheckAnyPermission(_companyGuid, undeclared));
            Assert.Throws<Authorization403ForbiddenException>(
                () => CreateService().CheckAnyPermission(_companyGuid, undeclared));
        }

        [Test]
        public void CheckAnyPermission_without_a_key_is_refused_instead_of_allowing_everything()
        {
            Assert.Throws<ArgumentException>(() => CreateService().CheckAnyPermission(_companyGuid));
            Assert.Throws<ArgumentException>(() => new HasPermissionAttribute());
        }

        [Test]
        public void CanEnterEndpoint_accepts_the_owner_of_a_company_without_a_role()
        {
            _companies.Items[0].OwnerGuid = _userGuid;
            _memberships.Items[0].IdentityRole = null;
            _memberships.Items[0].IdentityRoleId = null;

            Assert.That(CreateService().CanEnterEndpoint(new[] { EditTariff }), Is.True);
        }

        [Test]
        public void CanEnterEndpoint_refuses_a_request_without_a_current_user()
        {
            var service = new PermissionService(new FakeAspCurrentUserService(null),
                new HttpContextAccessor { HttpContext = _httpContext }, _memberships, _companies, _rolePermissions,
                new PermissionCatalog(new[] { new TestCatalog() }), new MemoryCache(new MemoryCacheOptions()),
                NullLogger<PermissionService>.Instance);

            Assert.That(service.CanEnterEndpoint(new[] { OpenBarrier }), Is.False);
        }

        private FasIdentityRole RoleWith(params string[] permissionKeys)
        {
            var role = new FasIdentityRole("Role" + _rolePermissions.Items.Count) { Id = Guid.NewGuid() };
            foreach (var key in permissionKeys)
                _rolePermissions.Items.Add(new RolePermission { RoleId = role.Id, PermissionKey = key });
            return role;
        }

        [Test]
        public void An_invalid_catalog_stops_the_application()
        {
            Assert.Throws<InvalidOperationException>(() => new PermissionCatalog(new IPermissionCatalogContributor[]
            {
                new TestCatalog(), new TestCatalog()
            }));
            Assert.Throws<InvalidOperationException>(() => new PermissionCatalog(new[]
            {
                new InlineCatalog("Not A Key")
            }));
        }

        [Test]
        public void Keys_can_be_taken_from_a_class_of_constants()
        {
            Assert.That(PermissionKeys.FromConstants(typeof(TestPermissions)),
                Is.EqualTo(new[] { "test.root.key", "test.nested.first", "test.nested.second" }));
        }

        private static class TestPermissions
        {
            public const string Root = "test.root.key";

            public static class Nested
            {
                public const string First = "test.nested.first";
                public const string Second = "test.nested.second";
                public static readonly string NotAConstant = "test.not.constant";
            }
        }

        private class TestCatalog : IPermissionCatalogContributor
        {
            public IEnumerable<string> GetPermissionKeys() => new[] { OpenBarrier, EditTariff, ViewPayments };
        }

        private class InlineCatalog : IPermissionCatalogContributor
        {
            private readonly string[] _keys;
            public InlineCatalog(params string[] keys) => _keys = keys;
            public IEnumerable<string> GetPermissionKeys() => _keys;
        }
    }
}
