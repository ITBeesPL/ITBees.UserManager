using System;
using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.RestfulApiControllers;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Helpers;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITBees.UserManager.Controllers
{
    [CustomControllerName("MyAccount")]
    [GenericRestControllerNameConvention]
    [Authorize]
    [Route("[controller]")]
    public class MyAccountController<T> : RestfulControllerBase<MyAccountController<T>> where T : IdentityUser<Guid>
    {
        private readonly IMyAccountServie _myAccountServie;
        private readonly IMyAccountUpdateService _myAccountUpdateService;
        private readonly ILoginService<T> _loginService;
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly RestrictedTokenOptions _restrictedTokenOptions;

        public MyAccountController(IMyAccountServie myAccountServie,
            IMyAccountUpdateService myAccountUpdateService,
            ILoginService<T> loginService,
            IAspCurrentUserService aspCurrentUserService,
            IOptions<RestrictedTokenOptions> restrictedTokenOptions,
            ILogger<MyAccountController<T>> logger) : base(logger)
        {
            _myAccountServie = myAccountServie;
            _myAccountUpdateService = myAccountUpdateService;
            _loginService = loginService;
            _aspCurrentUserService = aspCurrentUserService;
            _restrictedTokenOptions = restrictedTokenOptions.Value;
        }

        /// <summary>
        /// Returns information about currently logged in user
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Produces(typeof(MyAccountVm))]
        public IActionResult Get()
        {
            return ReturnOkResult(() => _myAccountServie.GetMyAccountData());
        }

        /// <summary>
        /// Updates the account of the current user and answers with a fresh login token, because the
        /// language and the selected company are part of the token.
        /// </summary>
        [HttpPut]
        [Produces(typeof(MyAccountWithTokenVm))]
        public async Task<IActionResult> Put([FromBody] MyAccountIm myAccountIm)
        {
            return await ReturnOkResultAsync(async () =>
            {
                // The answer is a regular login token of the token subject. A token an application
                // issued with narrowed-down rights must neither be exchanged for one nor change the
                // account - otherwise whoever holds it leaves with the full account.
                if (_restrictedTokenOptions.IsRestricted(User))
                {
                    throw new Authorization403ForbiddenException(
                        "This token is not allowed to update the account.");
                }

                // Both checks come before anything is saved.
                var cu = _aspCurrentUserService.GetCurrentUser();
                if (cu == null || myAccountIm.Email != cu.Email)
                {
                    throw new UnauthorizedAccessException("this will be reported");
                }

                _myAccountUpdateService.UpdateMyAccount(myAccountIm);
                MyAccountVm myAccount = _myAccountServie.GetMyAccountData();

                return await _loginService.GetMyAccountWithTokenWithoutAuthorization(myAccount,
                    myAccountIm.Language);
            });
        }
    }
}