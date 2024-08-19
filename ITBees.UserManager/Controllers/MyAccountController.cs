using System;
using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.MyAccount;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Services;
using ITBees.UserManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nelibur.ObjectMapper;

namespace ITBees.UserManager.Controllers
{
    [CustomControllerName("MyAccount")]
    [GenericRestControllerNameConvention]
    [Authorize]
    [Route("[controller]")]
    public class MyAccountController<T> : RestfulControllerBase<MyAccountController<T>> where T : IdentityUser
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
            try
            {
                MyAccount myAccount = _myAccountServie.GetMyAccountData();
                var result = TinyMapper.Map<MyAccountVm>(myAccount);
                return Ok(result);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, null);
            }
        }

        [HttpPut]
        [Produces(typeof(MyAccountVm))]
        public async Task<IActionResult> Put([FromBody] MyAccountIm myAccountIm)
        {
            try
            {
                _myAccountUpdateService.UpdateMyAccount(myAccountIm);

                MyAccount myAccount = _myAccountServie.GetMyAccountData();
                var cu = _aspCurrentUserService.GetCurrentUser();

                var newToken = await _loginService.LoginAfterEmailConfirmation(cu.Email, myAccountIm.Language);
                var result = new MyAccountVmWithToken(myAccount, newToken);
                return Ok(result);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, null);
            }
        }
    }
}