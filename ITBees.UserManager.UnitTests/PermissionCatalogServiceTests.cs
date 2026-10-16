using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Permissions;
using ITBees.UserManager.Services.Permissions;
using ITBees.UserManager.UnitTests.Fakes;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    public class PermissionCatalogServiceTests
    {
        private const string OpenBarrier = "test.barrier.open";
        private const string EditTariff = "test.tariff.edit";
        private const string NewKey = "test.new.key";

        [Test]
        public void Keys_come_from_code_names_and_groups_from_the_database_with_the_roles_holding_them()
        {
            var worker = new FasIdentityRole("Worker") { Id = Guid.NewGuid() };
            var owner = new FasIdentityRole("CompanyOwner") { Id = Guid.NewGuid() };
            var platformOperator = new FasIdentityRole("PlatformOperator") { Id = Guid.NewGuid() };
            var service = new PermissionCatalogService(
                new PermissionCatalog(new[] { new TestCatalog() }),
                new FakeRepository<Permission>(
                    new Permission { Key = EditTariff, Name = "Cenniki", GroupName = "Ustawienia", SortOrder = 1 },
                    new Permission { Key = OpenBarrier, Name = "Szlaban", GroupName = "Akcje", SortOrder = 0 },
                    new Permission { Key = "test.removed.key", Name = "Usunięty", GroupName = "Akcje", SortOrder = 2 }),
                new FakeRepository<RolePermission>(
                    new RolePermission { RoleId = worker.Id, PermissionKey = OpenBarrier },
                    new RolePermission { RoleId = owner.Id, PermissionKey = RolePermission.FullAccessKey }),
                new FakeRepository<FasIdentityRole>(worker, owner, platformOperator),
                new FakeAspCurrentUserService(new CurrentUser { Guid = Guid.NewGuid() }, isPlatformOperator: true));

            var catalog = service.Get();

            Assert.That(catalog.Groups.Select(x => x.Name),
                Is.EqualTo(new[] { "Akcje", "Ustawienia", PermissionVm.UndescribedGroupName }));
            var barrier = catalog.Groups[0].Permissions.Single();
            Assert.That((barrier.Key, barrier.Name), Is.EqualTo((OpenBarrier, "Szlaban")));
            Assert.That(barrier.RoleNames, Is.EqualTo(new[] { "Worker" }));
            Assert.That(catalog.Groups[2].Permissions.Single().Name, Is.EqualTo(NewKey),
                "a key without a database row is still listed");
            Assert.That(catalog.FullAccessRoleNames, Is.EqualTo(new[] { "CompanyOwner", "PlatformOperator" }));
        }

        private class TestCatalog : IPermissionCatalogContributor
        {
            public IEnumerable<string> GetPermissionKeys() => new[] { OpenBarrier, EditTariff, NewKey };
        }
    }
}
