using System;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.Services;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// LastUsedCompanyGuid ends up in the login token and applications scope their data by it, so
    /// PUT /MyAccount may only point an account at a company its user belongs to.
    /// </summary>
    public class MyAccountUpdateServiceTests
    {
        private readonly Guid _userGuid = Guid.NewGuid();
        private readonly Guid _ownCompany = Guid.NewGuid();
        private readonly Guid _secondOwnCompany = Guid.NewGuid();
        private readonly Guid _foreignCompany = Guid.NewGuid();

        private FakeRepository<UserAccount> _accounts;
        private MyAccountUpdateService _service;

        [SetUp]
        public void SetUp()
        {
            _accounts = new FakeRepository<UserAccount>(new UserAccount
            {
                Guid = _userGuid, Email = "user@test", FirstName = "Old", LastName = "Name", Phone = "1",
                LastUsedCompanyGuid = _ownCompany, LanguageId = 1
            });
            var memberships = new FakeRepository<UsersInCompany>(
                new UsersInCompany { UserAccountGuid = _userGuid, CompanyGuid = _ownCompany },
                new UsersInCompany { UserAccountGuid = _userGuid, CompanyGuid = _secondOwnCompany },
                new UsersInCompany { UserAccountGuid = Guid.NewGuid(), CompanyGuid = _foreignCompany });
            var languages = new FakeRepository<Language>(
                new Language { Id = 1, Code = "pl" }, new Language { Id = 2, Code = "en" });
            var currentUser = new CurrentUser
            {
                Guid = _userGuid, Email = "user@test", LastUsedCompanyGuid = _ownCompany, Language = new Pl()
            };

            _service = new MyAccountUpdateService(new FakeAspCurrentUserService(currentUser), _accounts,
                NullLogger<MyAccountUpdateService>.Instance, languages, memberships);
        }

        private MyAccountIm Request(Guid company, string language = "pl") => new MyAccountIm
        {
            Email = "user@test", FirstName = "New", LastName = "Name", Phone = "2",
            LastUsedCompanyGuid = company, Language = language
        };

        [Test]
        public void Switching_to_another_company_of_the_user_is_stored()
        {
            _service.UpdateMyAccount(Request(_secondOwnCompany));

            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_secondOwnCompany));
            Assert.That(_accounts.Items[0].FirstName, Is.EqualTo("New"));
        }

        [Test]
        public void Switching_to_a_company_the_user_does_not_belong_to_is_refused_and_nothing_is_saved()
        {
            Assert.Throws<Authorization403ForbiddenException>(() => _service.UpdateMyAccount(Request(_foreignCompany)));

            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_ownCompany));
            Assert.That(_accounts.Items[0].FirstName, Is.EqualTo("Old"), "the rest of the request must not be saved either");
            Assert.That(_accounts.UpdateCalls, Is.EqualTo(0));
        }

        [Test]
        public void A_company_that_does_not_exist_is_refused_like_a_foreign_one()
        {
            Assert.Throws<Authorization403ForbiddenException>(() => _service.UpdateMyAccount(Request(Guid.NewGuid())));

            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_ownCompany));
        }

        [Test]
        public void An_empty_company_guid_leaves_the_stored_company_and_saves_the_rest()
        {
            _service.UpdateMyAccount(Request(Guid.Empty));

            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_ownCompany));
            Assert.That(_accounts.Items[0].FirstName, Is.EqualTo("New"));
            Assert.That(_accounts.Items[0].Phone, Is.EqualTo("2"));
        }

        [Test]
        public void The_company_is_checked_on_the_language_change_path_as_well()
        {
            Assert.Throws<Authorization403ForbiddenException>(() =>
                _service.UpdateMyAccount(Request(_foreignCompany, language: "en")));
            Assert.That(_accounts.Items[0].LanguageId, Is.EqualTo(1));

            _service.UpdateMyAccount(Request(_secondOwnCompany, language: "en"));
            Assert.That(_accounts.Items[0].LanguageId, Is.EqualTo(2));
            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_secondOwnCompany));
        }
    }
}
