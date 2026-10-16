using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Permissions;
using ITBees.UserManager.Services.Permissions;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    public class UserRolePermissionsServiceTests
    {
        private const string OpenBarrier = "test.barrier.open";
        private const string EditTariff = "test.tariff.edit";

        private readonly FasIdentityRole _worker = new("Worker") { Id = Guid.NewGuid() };
        private readonly FasIdentityRole _platformOperator = new("PlatformOperator") { Id = Guid.NewGuid() };
        private FakeRepository<RolePermission> _rolePermissions;
        private MemoryCache _memoryCache;

        [SetUp]
        public void SetUp()
        {
            _rolePermissions = new FakeRepository<RolePermission>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        private UserRolePermissionsService CreateService(bool isPlatformOperator = true)
        {
            return new UserRolePermissionsService(new FakeRepository<FasIdentityRole>(_worker, _platformOperator),
                _rolePermissions, _rolePermissions, new PermissionCatalog(new[] { new TestCatalog() }),
                new FakeAspCurrentUserService(new CurrentUser { Guid = Guid.NewGuid(), Email = "op@test" },
                    isPlatformOperator),
                _memoryCache, NullLogger<UserRolePermissionsService>.Instance);
        }

        [Test]
        public void A_new_role_has_no_permissions()
        {
            var result = CreateService().Get(_worker.Id);

            Assert.That(result.FullAccess, Is.False);
            Assert.That(result.PermissionKeys, Is.Empty);
        }

        [Test]
        public void Saving_replaces_the_permissions_and_clears_the_cached_role()
        {
            _rolePermissions.Items.Add(new RolePermission { RoleId = _worker.Id, PermissionKey = OpenBarrier });
            _memoryCache.Set(PermissionService.GetRoleCacheKey(_worker.Id), new HashSet<string>());

            var result = CreateService().Update(new UserRolePermissionsUm
            {
                RoleGuid = _worker.Id, PermissionKeys = new List<string> { EditTariff, EditTariff }
            });

            Assert.That(result.PermissionKeys, Is.EquivalentTo(new[] { EditTariff }));
            Assert.That(_rolePermissions.Items.Select(x => x.PermissionKey), Is.EquivalentTo(new[] { EditTariff }));
            Assert.That(_memoryCache.TryGetValue(PermissionService.GetRoleCacheKey(_worker.Id), out _), Is.False);
        }

        [Test]
        public void Saving_writes_only_the_difference_so_a_failure_cannot_clear_the_role()
        {
            var kept = new RolePermission { RoleId = _worker.Id, PermissionKey = OpenBarrier };
            _rolePermissions.Items.Add(kept);

            CreateService().Update(new UserRolePermissionsUm
            {
                RoleGuid = _worker.Id, PermissionKeys = new List<string> { OpenBarrier, EditTariff }
            });

            Assert.That(_rolePermissions.Items.Select(x => x.PermissionKey),
                Is.EquivalentTo(new[] { OpenBarrier, EditTariff }));
            Assert.That(_rolePermissions.Items, Does.Contain(kept), "the row that stays is not deleted and re-inserted");
        }

        [Test]
        public void Saving_an_empty_list_leaves_the_role_without_permissions()
        {
            _rolePermissions.Items.Add(new RolePermission { RoleId = _worker.Id, PermissionKey = OpenBarrier });

            CreateService().Update(new UserRolePermissionsUm { RoleGuid = _worker.Id, PermissionKeys = new List<string>() });

            Assert.That(_rolePermissions.Items, Is.Empty);
        }

        [Test]
        public void Full_access_is_stored_as_a_single_marker_and_ignores_the_listed_keys()
        {
            var result = CreateService().Update(new UserRolePermissionsUm
            {
                RoleGuid = _worker.Id, FullAccess = true, PermissionKeys = new List<string> { OpenBarrier }
            });

            Assert.That(result.FullAccess, Is.True);
            Assert.That(_rolePermissions.Items.Select(x => x.PermissionKey),
                Is.EquivalentTo(new[] { RolePermission.FullAccessKey }));
            Assert.That(CreateService().Get(_worker.Id).FullAccess, Is.True);
        }

        [Test]
        public void Unknown_keys_are_refused_and_nothing_is_saved()
        {
            _rolePermissions.Items.Add(new RolePermission { RoleId = _worker.Id, PermissionKey = OpenBarrier });

            Assert.Throws<FasApiErrorException>(() => CreateService().Update(new UserRolePermissionsUm
            {
                RoleGuid = _worker.Id, PermissionKeys = new List<string> { EditTariff, "test.unknown.key" }
            }));
            Assert.That(_rolePermissions.Items.Select(x => x.PermissionKey), Is.EquivalentTo(new[] { OpenBarrier }));
        }

        [Test]
        public void The_full_access_marker_cannot_be_sent_as_a_key()
        {
            Assert.Throws<FasApiErrorException>(() => CreateService().Update(new UserRolePermissionsUm
            {
                RoleGuid = _worker.Id, PermissionKeys = new List<string> { RolePermission.FullAccessKey }
            }));
        }

        [Test]
        public void The_platform_operator_role_cannot_be_restricted()
        {
            Assert.That(CreateService().Get(_platformOperator.Id).FullAccess, Is.True);
            Assert.Throws<FasApiErrorException>(() => CreateService().Update(new UserRolePermissionsUm
            {
                RoleGuid = _platformOperator.Id, PermissionKeys = new List<string> { OpenBarrier }
            }));
        }

        [Test]
        public void Only_a_platform_operator_can_change_role_permissions()
        {
            Assert.Throws<Authorization403ForbiddenException>(() => CreateService(isPlatformOperator: false)
                .Update(new UserRolePermissionsUm { RoleGuid = _worker.Id, PermissionKeys = new List<string> { OpenBarrier } }));
        }

        private class TestCatalog : IPermissionCatalogContributor
        {
            public IEnumerable<string> GetPermissionKeys() => new[] { OpenBarrier, EditTariff };
        }
    }
}
