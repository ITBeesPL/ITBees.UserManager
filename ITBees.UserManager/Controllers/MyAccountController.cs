using System;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.MyAccount;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nelibur.ObjectMapper;

namespace ITBees.UserManager.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class MyAccountController : RestfulControllerBase<MyAccountController>
    {
        private readonly IMyAccountServie _myAccountServie;
        private readonly IMyAccountUpdateService _myAccountUpdateService;

        public MyAccountController(IMyAccountServie myAccountServie,
            IMyAccountUpdateService myAccountUpdateService,
            ILogger<MyAccountController> logger) : base(logger)
        {
            _myAccountServie = myAccountServie;
            _myAccountUpdateService = myAccountUpdateService;
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
        public IActionResult Put([FromBody] MyAccountIm myAccountIm)
        {
            try
            {
                _myAccountUpdateService.UpdateMyAccount(myAccountIm);
                MyAccount myAccount = _myAccountServie.GetMyAccountData();
                var result = TinyMapper.Map<MyAccountVm>(myAccount);
                return Ok(result);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, null);
            }
        }
    }
}