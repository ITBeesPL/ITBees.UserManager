using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Helpers;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.UnitTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// POST /RefreshToken answers with a regular login token of the token subject, so it must refuse a
    /// token that was issued with narrowed-down rights, and it issues nothing without a logged-in user.
    /// </summary>
    public class RefreshTokenControllerTests
    {
        private RecordingLoginService _loginService;

        private RefreshTokenController<IdentityUser<Guid>> Controller(RestrictedTokenOptions options,
            CurrentUser currentUser, params Claim[] tokenClaims)
        {
            _loginService = new RecordingLoginService();
            var controller = new RefreshTokenController<IdentityUser<Guid>>(_loginService,
                new FakeAspCurrentUserService(currentUser), Options.Create(options),
                NullLogger<RefreshTokenController<IdentityUser<Guid>>>.Instance);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(tokenClaims, "Bearer"))
                }
            };
            return controller;
        }

        private static CurrentUser LoggedInUser() => new CurrentUser
        {
            Guid = Guid.NewGuid(), Email = "user@test", Language = new Language { Id = 1, Code = "pl" }
        };

        private static int? StatusOf(IActionResult result) => (result as ObjectResult)?.StatusCode;

        [Test]
        public async Task A_regular_login_token_is_exchanged_for_a_fresh_one()
        {
            var controller = Controller(new RestrictedTokenOptions(), LoggedInUser(), new Claim("Email", "user@test"));

            var result = await controller.Post();

            Assert.That(StatusOf(result), Is.EqualTo(200));
            Assert.That(((TokenVm)((ObjectResult)result).Value).Value, Is.EqualTo("fresh-login-token"));
            Assert.That(_loginService.RefreshedEmail, Is.EqualTo("user@test"), "the token subject, never an email from the request");
            Assert.That(_loginService.RefreshedLanguage, Is.EqualTo("pl"), "the language of the current user");
        }

        [Test]
        public async Task A_token_with_the_TokenPurpose_claim_is_refused()
        {
            var controller = Controller(new RestrictedTokenOptions(), LoggedInUser(),
                new Claim("Email", "user@test"), new Claim(RestrictedTokenOptions.TokenPurposeClaimType, "internal-exam"));

            var result = await controller.Post();

            Assert.That(StatusOf(result), Is.EqualTo(403));
            Assert.That(_loginService.TokensIssued, Is.EqualTo(0), "a restricted token must not be exchanged for a login token");
        }

        [Test]
        public async Task An_application_can_mark_its_restricted_tokens_with_its_own_claim()
        {
            var options = new RestrictedTokenOptions();
            options.ClaimTypes.Add("KioskSession");
            var controller = Controller(options, LoggedInUser(), new Claim("Email", "user@test"), new Claim("KioskSession", "1"));

            var result = await controller.Post();

            Assert.That(StatusOf(result), Is.EqualTo(403));
            Assert.That(_loginService.TokensIssued, Is.EqualTo(0));
        }

        [Test]
        public async Task Without_a_logged_in_user_nothing_is_issued()
        {
            var controller = Controller(new RestrictedTokenOptions(), currentUser: null);

            var result = await controller.Post();

            Assert.That(StatusOf(result), Is.EqualTo(401));
            Assert.That(_loginService.TokensIssued, Is.EqualTo(0));
        }

        private class RecordingLoginService : ILoginService<IdentityUser<Guid>>
        {
            public int TokensIssued { get; private set; }
            public string RefreshedEmail { get; private set; }
            public string RefreshedLanguage { get; private set; }

            public Task<TokenVm> RefreshToken(string email, string lang)
            {
                TokensIssued++;
                RefreshedEmail = email;
                RefreshedLanguage = lang;
                return Task.FromResult(new TokenVm
                {
                    Value = "fresh-login-token", TokenExpirationDate = DateTime.UtcNow.AddMinutes(60)
                });
            }

            public Task<TokenVm> Login(string email, string pass, string lang) => throw new NotSupportedException();
            public Task<TokenVm> LoginAfterEmailConfirmation(string email, string lang) => throw new NotSupportedException();
            public Task<ConfirmEmailResult> ConfirmEmail(string googlePayloadEmail) => throw new NotSupportedException();
            public Task<MyAccountWithTokenVm> GetMyAccountWithTokenWithoutAuthorization(MyAccountVm myAccount, string language) => throw new NotSupportedException();
        }
    }
}
