using System;
using ITBees.Models.Companies;
using ITBees.Models.Roles;
using ITBees.Models.Users;
using ITBees.UserManager.Services.Acl;
using ITBees.UserManager.Services.Registration;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// Who may invite someone to a company: the platform operator, its owner or one of its members. Until now the
    /// service allowed everybody, so any logged-in user could invite people to any company.
    /// </summary>
    public class AccessControlServiceTests
    {
        private static readonly Type Service = typeof(NewUserRegistrationService<IdentityUser<Guid>, Company>);
        private const string Method = nameof(NewUserRegistrationService<IdentityUser<Guid>, Company>.CreateAndInviteNewUserToCompany);

        private readonly Guid _userGuid = Guid.NewGuid();
        private readonly Guid _companyGuid = Guid.NewGuid();
        private readonly Guid _otherCompanyGuid = Guid.NewGuid();

        private FakeRepository<UsersInCompany> _memberships;
        private FakeRepository<Company> _companies;

        [SetUp]
        public void SetUp()
        {
            _memberships = new FakeRepository<UsersInCompany>(
                new UsersInCompany { UserAccountGuid = _userGuid, CompanyGuid = _otherCompanyGuid });
            _companies = new FakeRepository<Company>(
                new Company { Guid = _companyGuid, OwnerGuid = Guid.NewGuid() },
                new Company { Guid = _otherCompanyGuid, OwnerGuid = Guid.NewGuid() });
        }

        private bool CanInvite(CurrentUser user, Guid companyGuid) =>
            new AccessControlService(_memberships, _companies, NullLogger<AccessControlService>.Instance)
                .CanDo(user, Service, Method, companyGuid).CanDoResult;

        private CurrentUser User(params string[] roles) => new() { Guid = _userGuid, UserRoles = roles };

        [Test]
        public void Someone_outside_the_company_may_not_invite_to_it()
        {
            Assert.That(CanInvite(User(), _companyGuid), Is.False);
        }

        [Test]
        public void A_member_may_invite_to_his_company()
        {
            _memberships.Items.Add(new UsersInCompany { UserAccountGuid = _userGuid, CompanyGuid = _companyGuid });

            Assert.That(CanInvite(User(), _companyGuid), Is.True);
        }

        [Test]
        public void The_owner_may_invite_to_his_company()
        {
            _companies.Items[0].OwnerGuid = _userGuid;

            Assert.That(CanInvite(User(), _companyGuid), Is.True);
        }

        [Test]
        public void The_creator_of_a_company_without_an_owner_may_invite_to_it()
        {
            _companies.Items[0].OwnerGuid = null;
            _companies.Items[0].CreatedByGuid = _userGuid;

            Assert.That(CanInvite(User(), _companyGuid), Is.True);
        }

        [Test]
        public void The_creator_of_a_company_owned_by_someone_else_may_not_invite_to_it()
        {
            _companies.Items[0].CreatedByGuid = _userGuid;

            Assert.That(CanInvite(User(), _companyGuid), Is.False);
        }

        [Test]
        public void The_platform_operator_may_invite_to_any_company()
        {
            Assert.That(CanInvite(User(Role.PlatformOperator), _companyGuid), Is.True);
        }

        [Test]
        public void Nobody_may_invite_without_logging_in()
        {
            Assert.That(CanInvite(null, _companyGuid), Is.False);
        }
    }
}
