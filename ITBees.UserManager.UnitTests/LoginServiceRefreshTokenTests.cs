using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ITBees.BaseServices.Platforms.Interfaces;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
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
    /// POST /RefreshToken exchanges a valid login token for a fresh one of the same user. The refreshed
    /// token has to say the same things as a login token, only later - and a refresh is not a login.
    /// </summary>
    public class LoginServiceRefreshTokenTests
    {
        private const string Email = "user@test";
        private const int TokenLifeTimeMinutes = 60;
        private static readonly DateTime Now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        private readonly Guid _userGuid = Guid.NewGuid();
        private readonly Guid _company = Guid.NewGuid();
        private readonly Guid _foreignCompany = Guid.NewGuid();

        private FakeRepository<UserAccount> _accounts;
        private string _translationsPath;

        [SetUp]
        public void SetUp()
        {
            // The refusal of an unknown account is translated, and this key has no built-in default.
            _translationsPath = Path.Combine(Path.GetTempPath(), "refresh-token-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_translationsPath);
            File.WriteAllText(Path.Combine(_translationsPath, "en.json"),
                "{\"UserManager\": {\"UserLogin\": {" +
                "\"ITBees.UserManager.Translations.UserManager.UserLogin.EmailNotRegistered\": \"Email not registered\"}}}");
            Translate.ClearTranslations();
            Translate.LoadFiles(_translationsPath);
        }

        [TearDown]
        public void TearDown()
        {
            Translate.ClearTranslations();
            Directory.Delete(_translationsPath, true);
        }

        private LoginService<IdentityUser<Guid>> Service(Guid? storedCompany = null, int loginsCount = 5)
        {
            var membership = new UsersInCompany
            {
                UserAccountGuid = _userGuid, CompanyGuid = _company, AddedDate = Now.AddDays(-30)
            };
            var account = new UserAccount
            {
                Guid = _userGuid, Email = Email, FirstName = "U", LastName = "S",
                LastUsedCompanyGuid = storedCompany ?? _company,
                Language = new Language { Id = 1, Code = "pl" }, LanguageId = 1,
                LoginsCount = loginsCount, LastLoginDateTime = Now.AddDays(-10),
                UserAccountModules = new List<UserAccountModule>(),
                UsersInCompanies = new List<UsersInCompany> { membership }
            };
            _accounts = new FakeRepository<UserAccount>(account);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TokenLifeTime"] = TokenLifeTimeMinutes.ToString(),
                ["TokenSecretKey"] = "unit-test-secret-key-0123456789-0123456789-0123456789",
                ["TokenIssuer"] = "unit-tests",
                ["TokenAudience"] = "unit-tests"
            }).Build();

            var userManager = Stub.Of<IUserManager<IdentityUser<Guid>>>(
                ("FindByEmailAsync", args => Task.FromResult(new IdentityUser<Guid> { Email = (string)args[0] })),
                ("GetRolesAsync", _ => Task.FromResult<IList<string>>(new List<string> { "User" })));
            var clock = Stub.Of<ICurrentDateTimeService>(("GetCurrentDate", _ => Now));

            return new LoginService<IdentityUser<Guid>>(
                userManager, _accounts, new FakeRepository<UsersInCompany>(membership), configuration,
                _accounts, clock, Stub.Of<ICompanyBootstrapService>(),
                NullLogger<LoginService<IdentityUser<Guid>>>.Instance);
        }

        private static JwtSecurityToken Read(string jwt) => new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        // jti is unique to a token and exp/nbf/iat are its times - everything else has to match.
        private static readonly string[] PerTokenClaims = { "jti", "exp", "nbf", "iat" };

        private static List<string> ComparableClaimsOf(string jwt) => Read(jwt).Claims
            .Where(x => !PerTokenClaims.Contains(x.Type))
            .Select(x => $"{x.Type}={x.Value}").OrderBy(x => x).ToList();

        [Test]
        public async Task The_refreshed_token_has_the_same_subject_and_claims_as_the_login_token()
        {
            var service = Service();
            var login = await service.LoginAfterEmailConfirmation(Email, "pl");

            var refreshed = await service.RefreshToken(Email, "pl");

            Assert.That(Read(refreshed.Value).Subject, Is.EqualTo(_userGuid.ToString()));
            Assert.That(ComparableClaimsOf(refreshed.Value), Is.EqualTo(ComparableClaimsOf(login.Value)));
            Assert.That(ComparableClaimsOf(refreshed.Value), Does.Contain($"LastUsedCompanyGuid={_company}")
                .And.Contain("Language=pl").And.Contain($"Email={Email}").And.Contain("Roles=User"));
            Assert.That(refreshed.Value, Is.Not.EqualTo(login.Value), "a new token, not the old one handed back");
        }

        [Test]
        public async Task The_refreshed_token_expires_a_full_token_lifetime_from_now()
        {
            var service = Service();
            var login = await service.LoginAfterEmailConfirmation(Email, "pl");
            var before = DateTime.UtcNow;

            var refreshed = await service.RefreshToken(Email, "pl");

            // The JWT keeps whole seconds, so the expiry may land up to a second before "before + lifetime".
            Assert.That(refreshed.TokenExpirationDate, Is.InRange(
                before.AddMinutes(TokenLifeTimeMinutes).AddSeconds(-1),
                DateTime.UtcNow.AddMinutes(TokenLifeTimeMinutes)));
            Assert.That(refreshed.TokenExpirationDate, Is.GreaterThanOrEqualTo(login.TokenExpirationDate));
        }

        [Test]
        public async Task A_refresh_is_not_a_login()
        {
            var service = Service(loginsCount: 5);

            await service.RefreshToken(Email, "pl");

            Assert.That(_accounts.Items[0].LoginsCount, Is.EqualTo(5), "LoginsCount counts logins, not refreshes");
            Assert.That(_accounts.Items[0].LastLoginDateTime, Is.EqualTo(Now), "the refresh is the last activity of the account");

            await service.LoginAfterEmailConfirmation(Email, "pl");

            Assert.That(_accounts.Items[0].LoginsCount, Is.EqualTo(6), "a login still counts");
        }

        [Test]
        public async Task A_company_the_user_no_longer_belongs_to_never_reaches_the_refreshed_token()
        {
            var service = Service(storedCompany: _foreignCompany);

            var refreshed = await service.RefreshToken(Email, "pl");

            Assert.That(Read(refreshed.Value).Claims.Single(x => x.Type == "LastUsedCompanyGuid").Value,
                Is.EqualTo(_company.ToString()));
            Assert.That(_accounts.Items[0].LastUsedCompanyGuid, Is.EqualTo(_company));
        }

        [Test]
        public void An_account_that_no_longer_exists_cannot_be_refreshed()
        {
            var service = Service();

            // The language only picks the text of the refusal; "en" is the one the loaded translation set has.
            Assert.ThrowsAsync<ArgumentException>(() => service.RefreshToken("gone@test", "en"));
        }
    }
}
