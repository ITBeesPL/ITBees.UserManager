using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using ITBees.BaseServices.Platforms.Interfaces;
using ITBees.Models.Companies;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// The LastUsedCompanyGuid claim is what applications scope their data by. Every token the
    /// platform issues has to name a company its user belongs to at that moment.
    /// </summary>
    public class LoginServiceLastUsedCompanyTests
    {
        private const string Email = "user@test";
        private readonly Guid _userGuid = Guid.NewGuid();
        private readonly Guid _oldestCompany = Guid.NewGuid();
        private readonly Guid _newerCompany = Guid.NewGuid();
        private readonly Guid _foreignCompany = Guid.NewGuid();

        private FakeRepository<UserAccount> _accounts;

        private LoginService<IdentityUser<Guid>> ServiceFor(Guid? storedCompany, params UsersInCompany[] memberships)
        {
            var account = new UserAccount
            {
                Guid = _userGuid, Email = Email, FirstName = "U", LastName = "S", LastUsedCompanyGuid = storedCompany,
                Language = new Language { Id = 1, Code = "pl" }, LanguageId = 1,
                UserAccountModules = new List<UserAccountModule>(),
                UsersInCompanies = memberships.ToList()
            };
            _accounts = new FakeRepository<UserAccount>(account);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TokenLifeTime"] = "60",
                ["TokenSecretKey"] = "unit-test-secret-key-0123456789-0123456789-0123456789",
                ["TokenIssuer"] = "unit-tests",
                ["TokenAudience"] = "unit-tests"
            }).Build();

            return new LoginService<IdentityUser<Guid>>(
                new StubUserManager(), _accounts, new FakeRepository<UsersInCompany>(memberships), configuration,
                _accounts, new FixedClock(), new NoBootstrap(), NullLogger<LoginService<IdentityUser<Guid>>>.Instance);
        }

        private UsersInCompany Membership(Guid company, int addedDaysAgo) => new UsersInCompany
        {
            UserAccountGuid = _userGuid, CompanyGuid = company, AddedDate = DateTime.UtcNow.AddDays(-addedDaysAgo)
        };

        private static Guid CompanyClaimOf(string jwt) => Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(jwt)
            .Claims.Single(x => x.Type == "LastUsedCompanyGuid").Value);

        [Test]
        public async Task A_company_the_user_belongs_to_is_kept()
        {
            var service = ServiceFor(_newerCompany, Membership(_oldestCompany, 30), Membership(_newerCompany, 1));

            var token = await service.LoginAfterEmailConfirmation(Email, "pl");

            Assert.That(CompanyClaimOf(token.Value), Is.EqualTo(_newerCompany));
            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_newerCompany));
        }

        [Test]
        public async Task A_stored_company_the_user_does_not_belong_to_never_reaches_the_token()
        {
            // Stored through the old PUT /MyAccount, or left behind after the user was removed from the company.
            var service = ServiceFor(_foreignCompany, Membership(_newerCompany, 1), Membership(_oldestCompany, 30));

            var token = await service.LoginAfterEmailConfirmation(Email, "pl");

            Assert.That(CompanyClaimOf(token.Value), Is.EqualTo(_oldestCompany), "falls back to the oldest company of the user");
            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_oldestCompany), "and the account stops pointing at the foreign company");
        }

        [Test]
        public async Task A_missing_company_still_falls_back_to_a_company_of_the_user()
        {
            var service = ServiceFor(null, Membership(_newerCompany, 1), Membership(_oldestCompany, 30));

            var token = await service.LoginAfterEmailConfirmation(Email, "pl");

            Assert.That(CompanyClaimOf(token.Value), Is.EqualTo(_oldestCompany));
        }

        private class StubUserManager : IUserManager<IdentityUser<Guid>>
        {
            public Task<IdentityUser<Guid>> FindByEmailAsync(string email) => Task.FromResult(new IdentityUser<Guid> { Email = email });
            public Task<IList<string>> GetRolesAsync(object user) => Task.FromResult<IList<string>>(new List<string>());

            public Task<IdentityResult> CreateAsync(object user, string password) => throw new NotSupportedException();
            public Task<bool> CheckPasswordAsync(object user, string password) => throw new NotSupportedException();
            public Task<string> GenerateEmailConfirmationTokenAsync(dynamic user) => throw new NotSupportedException();
            public Task<IdentityResult> ConfirmEmailAsync(object user, string token) => throw new NotSupportedException();
            public Task<IdentityResult> AddToRoleAsync(object user, string role) => throw new NotSupportedException();
            public Task<IdentityResult> ResetPasswordAsync(object user, string token, string newPassword) => throw new NotSupportedException();
            public Task<string> GeneratePasswordResetTokenAsync(object user) => throw new NotSupportedException();
            public Task<IdentityUser<Guid>> FindByIdAsync(string userIDGuid) => throw new NotSupportedException();
            public Task<IdentityResult> ChangePasswordAsync(object user, string currentPass, string newPass) => throw new NotSupportedException();
            public Task DeleteAccount(bool leaveAccountGuidForFutureBillingInformation, Guid userGuid) => throw new NotSupportedException();
        }

        private class FixedClock : ICurrentDateTimeService
        {
            public DateTime GetCurrentDate() => new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        }

        private class NoBootstrap : ICompanyBootstrapService
        {
            public Company EnsureUserHasCompany(Guid userAccountGuid, string desiredCompanyName, Language userLanguage)
                => throw new InvalidOperationException("The user has companies - no bootstrap expected.");
        }
    }
}
