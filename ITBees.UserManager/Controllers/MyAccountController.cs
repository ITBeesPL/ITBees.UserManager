using System;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.MyAccount;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces;
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

        public MyAccountController(IMyAccountServie myAccountServie, ILogger<MyAccountController> logger) : base(logger)
        {
            _myAccountServie = myAccountServie;
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
    }
}