using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.Companies;
using ITBees.FAS.ApiInterfaces.MyAccounts;
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

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// PUT /MyAccount answers with a regular login token of the token subject, so it must refuse a
    /// token that was issued with narrowed-down rights, and it must not save anything before its
    /// checks are done.
    /// </summary>
    public class MyAccountControllerTests
    {
        private RecordingUpdateService _updateService;
        private RecordingLoginService _loginService;

        private MyAccountController<IdentityUser<Guid>> Controller(RestrictedTokenOptions options,
            params Claim[] tokenClaims)
        {
            _updateService = new RecordingUpdateService();
            _loginService = new RecordingLoginService();
            var currentUser = new CurrentUser { Guid = Guid.NewGuid(), Email = "user@test" };
            var controller = new MyAccountController<IdentityUser<Guid>>(
                new StubMyAccountService(), _updateService, _loginService,
                new FakeAspCurrentUserService(currentUser), Options.Create(options),
                NullLogger<MyAccountController<IdentityUser<Guid>>>.Instance);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(tokenClaims, "Bearer"))
                }
            };
            return controller;
        }

        private static MyAccountIm Request(string email = "user@test") => new MyAccountIm
        {
            Email = email, FirstName = "A", LastName = "B", Language = "pl", LastUsedCompanyGuid = Guid.NewGuid()
        };

        private static int? StatusOf(IActionResult result) => (result as ObjectResult)?.StatusCode;

        [Test]
        public async Task A_regular_login_token_updates_the_account_and_gets_a_fresh_token()
        {
            var controller = Controller(new RestrictedTokenOptions(), new Claim("Email", "user@test"));

            var result = await controller.Put(Request());

            Assert.That(StatusOf(result), Is.EqualTo(200));
            Assert.That(_updateService.Calls, Is.EqualTo(1));
            Assert.That(_loginService.TokensIssued, Is.EqualTo(1));
            Assert.That(((MyAccountWithTokenVm)((ObjectResult)result).Value).TokenVm.Value, Is.EqualTo("fresh-login-token"));
        }

        [Test]
        public async Task A_token_with_the_TokenPurpose_claim_is_refused_before_anything_happens()
        {
            var controller = Controller(new RestrictedTokenOptions(),
                new Claim("Email", "user@test"), new Claim(RestrictedTokenOptions.TokenPurposeClaimType, "internal-exam"));

            var result = await controller.Put(Request());

            Assert.That(StatusOf(result), Is.EqualTo(403));
            Assert.That(_updateService.Calls, Is.EqualTo(0), "a restricted token must not change the account");
            Assert.That(_loginService.TokensIssued, Is.EqualTo(0), "a restricted token must not be exchanged for a login token");
        }

        [Test]
        public async Task An_application_can_mark_its_restricted_tokens_with_its_own_claim()
        {
            var options = new RestrictedTokenOptions();
            options.ClaimTypes.Add("KioskSession");
            var controller = Controller(options, new Claim("Email", "user@test"), new Claim("KioskSession", "1"));

            var result = await controller.Put(Request());

            Assert.That(StatusOf(result), Is.EqualTo(403));
            Assert.That(_updateService.Calls, Is.EqualTo(0));
            Assert.That(_loginService.TokensIssued, Is.EqualTo(0));
        }

        [Test]
        public async Task A_foreign_email_in_the_request_is_refused_before_the_account_is_saved()
        {
            var controller = Controller(new RestrictedTokenOptions(), new Claim("Email", "user@test"));

            var result = await controller.Put(Request(email: "someone-else@test"));

            Assert.That(StatusOf(result), Is.EqualTo(401));
            Assert.That(_updateService.Calls, Is.EqualTo(0), "the check used to run after the update had been saved");
            Assert.That(_loginService.TokensIssued, Is.EqualTo(0));
        }

        private class RecordingUpdateService : IMyAccountUpdateService
        {
            public int Calls { get; private set; }
            public void UpdateMyAccount(MyAccountIm myAccountIm) => Calls++;
        }

        private class StubMyAccountService : IMyAccountServie
        {
            public MyAccountVm GetMyAccountData() => new MyAccountVm
            {
                Email = "user@test", Language = "pl", Companies = new List<CompanyWithUserRoleVm>()
            };
        }

        private class RecordingLoginService : ILoginService<IdentityUser<Guid>>
        {
            public int TokensIssued { get; private set; }

            public Task<MyAccountWithTokenVm> GetMyAccountWithTokenWithoutAuthorization(MyAccountVm myAccount, string language)
            {
                TokensIssued++;
                return Task.FromResult(new MyAccountWithTokenVm(myAccount, new TokenVm { Value = "fresh-login-token" }));
            }

            public Task<TokenVm> Login(string email, string pass, string lang) => throw new NotSupportedException();
            public Task<TokenVm> LoginAfterEmailConfirmation(string email, string lang) => throw new NotSupportedException();
            public Task<ConfirmEmailResult> ConfirmEmail(string googlePayloadEmail) => throw new NotSupportedException();
        }
    }
}
