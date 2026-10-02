using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.AppleLogins;
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
    /// Apple leaves the email out of the token for some accounts. The "sub" claim is the only stable
    /// identifier, so an account that has signed in with Apple once has to be found by it.
    /// </summary>
    public class AppleLoginServiceTests
    {
        private const string Email = "student@test";
        private const string AppleSub = "001234.abcdef.5678";

        private string _translationsPath;
        private FakeUserManager _userManager;
        private RecordingLoginService _loginService;
        private RecordingRegistration _registration;
        private FakeRepository<UserAccount> _accounts;

        [SetUp]
        public void SetUp()
        {
            // An application without the new text in its translation files - the English default is expected.
            _translationsPath = Path.Combine(Path.GetTempPath(), "apple-login-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_translationsPath);
            File.WriteAllText(Path.Combine(_translationsPath, "en.json"), "{}");
            Translate.ClearTranslations();
            Translate.LoadFiles(_translationsPath);
        }

        [TearDown]
        public void TearDown()
        {
            Translate.ClearTranslations();
            Directory.Delete(_translationsPath, true);
        }

        private AppleLoginService<IdentityUser<Guid>> ServiceFor(params string[] registeredEmails)
        {
            _userManager = new FakeUserManager(registeredEmails);
            _loginService = new RecordingLoginService();
            _registration = new RecordingRegistration(_userManager);
            _accounts = new FakeRepository<UserAccount>(registeredEmails
                .Select(x => new UserAccount { Guid = Guid.NewGuid(), Email = x }).ToArray());

            return new AppleLoginService<IdentityUser<Guid>>(null, null, _loginService,
                NullLogger<AppleLoginService<IdentityUser<Guid>>>.Instance, _accounts, _registration, _userManager);
        }

        private static AppleTokenResponse TokenFromApple(string sub, string email)
        {
            var claims = new List<Claim> { new Claim("sub", sub) };
            if (email != null)
            {
                claims.Add(new Claim("email", email));
                claims.Add(new Claim("email_verified", "true"));
            }

            var jwt = new JwtSecurityToken("https://appleid.apple.com", "com.example.app", claims,
                expires: DateTime.UtcNow.AddMinutes(5));
            return new AppleTokenResponse { IdToken = new JwtSecurityTokenHandler().WriteToken(jwt) };
        }

        [Test]
        public void A_token_without_email_and_without_a_linked_account_is_refused_with_an_explanation()
        {
            var service = ServiceFor(Email);

            var error = Assert.ThrowsAsync<ArgumentException>(() => service.LoginOrRegister(TokenFromApple(AppleSub, null), "en"));

            Assert.That(error.Message, Does.StartWith("Apple did not share your email address"));
            Assert.That(_registration.RegisteredEmails, Is.Empty, "no account is created with an empty email");
            Assert.That(_loginService.LoggedInEmails, Is.Empty);
        }

        [Test]
        public async Task Signing_in_with_an_email_links_the_apple_account_to_the_existing_account()
        {
            var service = ServiceFor(Email);

            var token = await service.LoginOrRegister(TokenFromApple(AppleSub, Email), "en");

            Assert.That(token.Value, Is.EqualTo("token-for-" + Email));
            Assert.That(_registration.RegisteredEmails, Is.Empty);
            Assert.That(_userManager.EmailLinkedTo(AppleSub), Is.EqualTo(Email));
        }

        [Test]
        public async Task A_new_user_is_registered_and_linked()
        {
            var service = ServiceFor();

            await service.LoginOrRegister(TokenFromApple(AppleSub, Email), "en");

            Assert.That(_registration.RegisteredEmails, Is.EqualTo(new[] { Email }));
            Assert.That(_userManager.EmailLinkedTo(AppleSub), Is.EqualTo(Email));
        }

        [Test]
        public async Task A_linked_account_signs_in_when_apple_sends_no_email()
        {
            var service = ServiceFor(Email);
            await service.LoginOrRegister(TokenFromApple(AppleSub, Email), "en");
            _loginService.LoggedInEmails.Clear();

            var token = await service.LoginOrRegister(TokenFromApple(AppleSub, null), "en");

            Assert.That(token.Value, Is.EqualTo("token-for-" + Email));
            Assert.That(_loginService.LoggedInEmails, Is.EqualTo(new[] { Email }));
        }

        [Test]
        public async Task A_linked_account_is_kept_when_the_email_at_apple_changes()
        {
            var service = ServiceFor(Email);
            await service.LoginOrRegister(TokenFromApple(AppleSub, Email), "en");
            _loginService.LoggedInEmails.Clear();

            await service.LoginOrRegister(TokenFromApple(AppleSub, "new-address@test"), "en");

            Assert.That(_loginService.LoggedInEmails, Is.EqualTo(new[] { Email }));
            Assert.That(_registration.RegisteredEmails, Is.Empty, "a changed email must not create a second account");
        }

        [Test]
        public async Task A_failing_link_does_not_stop_the_sign_in()
        {
            var service = ServiceFor(Email);
            _userManager.LinkingFails = true;

            var token = await service.LoginOrRegister(TokenFromApple(AppleSub, Email), "en");

            Assert.That(token.Value, Is.EqualTo("token-for-" + Email));
        }

        private class FakeUserManager : IUserManager<IdentityUser<Guid>>
        {
            private readonly Dictionary<string, IdentityUser<Guid>> _usersByEmail = new();
            private readonly Dictionary<string, IdentityUser<Guid>> _usersByLogin = new();

            public bool LinkingFails { get; set; }

            public FakeUserManager(IEnumerable<string> emails)
            {
                foreach (var email in emails) Add(email);
            }

            public void Add(string email) => _usersByEmail[email] = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = email };

            public string EmailLinkedTo(string providerKey) =>
                _usersByLogin.TryGetValue($"{AppleLoginService<IdentityUser<Guid>>.AppleLoginProvider}|{providerKey}", out var user) ? user.Email : null;

            public Task<IdentityUser<Guid>> FindByEmailAsync(string email) =>
                Task.FromResult(email != null && _usersByEmail.TryGetValue(email, out var user) ? user : null);

            public Task<IdentityUser<Guid>> FindByLoginAsync(string loginProvider, string providerKey) =>
                Task.FromResult(_usersByLogin.TryGetValue($"{loginProvider}|{providerKey}", out var user) ? user : null);

            public Task<IdentityResult> AddLoginAsync(object user, string loginProvider, string providerKey)
            {
                if (LinkingFails) throw new InvalidOperationException("The logins table is not there.");
                _usersByLogin[$"{loginProvider}|{providerKey}"] = (IdentityUser<Guid>)user;
                return Task.FromResult(IdentityResult.Success);
            }

            public Task<IList<string>> GetRolesAsync(object user) => throw new NotSupportedException();
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

        private class RecordingLoginService : ILoginService<IdentityUser<Guid>>
        {
            public List<string> LoggedInEmails { get; } = new();

            public Task<TokenVm> LoginAfterEmailConfirmation(string email, string lang)
            {
                LoggedInEmails.Add(email);
                return Task.FromResult(new TokenVm { Value = "token-for-" + email });
            }

            public Task<ConfirmEmailResult> ConfirmEmail(string email) => Task.FromResult(new ConfirmEmailResult { Success = true });
            public Task<TokenVm> Login(string email, string pass, string lang) => throw new NotSupportedException();
            public Task<MyAccountWithTokenVm> GetMyAccountWithTokenWithoutAuthorization(MyAccountVm email, string language) => throw new NotSupportedException();
        }

        private class RecordingRegistration : INewUserRegistrationFromApple
        {
            private readonly FakeUserManager _userManager;
            public List<string> RegisteredEmails { get; } = new();

            public RecordingRegistration(FakeUserManager userManager)
            {
                _userManager = userManager;
            }

            public Task<TokenVm> CreateNewUserAccountFromAppleLogin(AppleTokenClaims appleTokenClaims)
            {
                RegisteredEmails.Add(appleTokenClaims.Email);
                _userManager.Add(appleTokenClaims.Email);
                return Task.FromResult(new TokenVm { Value = "token-for-" + appleTokenClaims.Email });
            }
        }
    }
}
