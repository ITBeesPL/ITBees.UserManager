using System;
using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

        public MyAccountController(IMyAccountServie myAccountServie,
            IMyAccountUpdateService myAccountUpdateService,
            ILoginService<T> loginService,
            IAspCurrentUserService aspCurrentUserService,
            ILogger<MyAccountController<T>> logger) : base(logger)
        {
            _myAccountServie = myAccountServie;
            _myAccountUpdateService = myAccountUpdateService;
            _loginService = loginService;
            _aspCurrentUserService = aspCurrentUserService;
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

        [HttpPut]
        [Produces(typeof(MyAccountWithTokenVm))]
        public async Task<IActionResult> Put([FromBody] MyAccountIm myAccountIm)
        {
            return await ReturnOkResultAsync(async () =>
            {
                _myAccountUpdateService.UpdateMyAccount(myAccountIm);
                MyAccountVm myAccount = _myAccountServie.GetMyAccountData();
                var cu = _aspCurrentUserService.GetCurrentUser();
                if (myAccountIm.Email != cu.Email)
                {
                    throw new UnauthorizedAccessException("this will be reported");
                }

                return await _loginService.GetMyAccountWithTokenWithoutAuthorization(myAccount,
                    myAccountIm.Language);
            });
        }
    }
}