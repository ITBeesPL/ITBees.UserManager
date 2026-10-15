using System;
using System.Linq;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Services;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// A role that someone holds is deleted only together with the decision which role they get instead.
    /// </summary>
    public class UserRolesServiceDeleteTests
    {
        private readonly FasIdentityRole _cashier = new("Cashier") { Id = Guid.NewGuid() };
        private readonly FasIdentityRole _worker = new("Worker") { Id = Guid.NewGuid() };
        private readonly FasIdentityRole _platformOperator = new("PlatformOperator") { Id = Guid.NewGuid() };
        private readonly Guid _employee = Guid.NewGuid();
        private readonly Guid _invited = Guid.NewGuid();
        private readonly Guid _globalHolder = Guid.NewGuid();

        private FakeRepository<FasIdentityRole> _roles;
        private FakeRepository<UsersInCompany> _memberships;
        private FakeRepository<UsersInvitationsToCompanies> _invitations;
        private FakeRepository<IdentityUserRole<Guid>> _globalRoles;

        [SetUp]
        public void SetUp()
        {
            _roles = new FakeRepository<FasIdentityRole>(_cashier, _worker, _platformOperator);
            _memberships = new FakeRepository<UsersInCompany>(
                new UsersInCompany { UserAccountGuid = _employee, IdentityRoleId = _cashier.Id });
            _invitations = new FakeRepository<UsersInvitationsToCompanies>(
                new UsersInvitationsToCompanies { UserAccountGuid = _invited, IdentityRoleId = _cashier.Id });
            _globalRoles = new FakeRepository<IdentityUserRole<Guid>>(
                new IdentityUserRole<Guid> { UserId = _globalHolder, RoleId = _cashier.Id },
                new IdentityUserRole<Guid> { UserId = _employee, RoleId = _worker.Id });
        }

        private UserRolesService CreateService() => new(_roles, _roles, _memberships, _memberships,
            _invitations, _invitations, _globalRoles, _globalRoles,
            new FakeAspCurrentUserService(new CurrentUser { Guid = Guid.NewGuid() }, isPlatformOperator: true),
            NullLogger<UserRolesService>.Instance);

        [Test]
        public void The_role_reports_how_many_users_hold_it()
        {
            Assert.That(CreateService().GetRole(_cashier.Id).AssignedUsersCount, Is.EqualTo(3));
        }

        [Test]
        public void A_held_role_is_not_deleted_without_a_replacement()
        {
            Assert.Throws<FasApiErrorException>(() => CreateService().Delete(_cashier.Id, null));
            Assert.That(_roles.Items, Does.Contain(_cashier));
        }

        [Test]
        public void Holders_get_the_replacement_role_and_the_role_is_deleted()
        {
            CreateService().Delete(_cashier.Id, _worker.Id);

            Assert.That(_roles.Items, Does.Not.Contain(_cashier));
            Assert.That(_memberships.Items.Single().IdentityRoleId, Is.EqualTo(_worker.Id));
            Assert.That(_invitations.Items.Single().IdentityRoleId, Is.EqualTo(_worker.Id));
            Assert.That(_globalRoles.Items.Select(x => (x.UserId, x.RoleId)), Is.EquivalentTo(new[]
            {
                (_globalHolder, _worker.Id),
                (_employee, _worker.Id)
            }));
        }

        [Test]
        public void The_platform_operator_role_cannot_be_the_replacement()
        {
            Assert.Throws<FasApiErrorException>(() => CreateService().Delete(_cashier.Id, _platformOperator.Id));
            Assert.That(_memberships.Items.Single().IdentityRoleId, Is.EqualTo(_cashier.Id));
        }

        [Test]
        public void A_role_nobody_holds_is_deleted_without_a_replacement()
        {
            _memberships.Items.Clear();
            _invitations.Items.Clear();
            _globalRoles.Items.RemoveAll(x => x.RoleId == _cashier.Id);

            CreateService().Delete(_cashier.Id, null);

            Assert.That(_roles.Items, Does.Not.Contain(_cashier));
        }
    }
}
