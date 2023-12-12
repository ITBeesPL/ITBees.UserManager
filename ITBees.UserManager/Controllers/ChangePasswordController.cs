using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Services.Passwords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [Authorize]
    public class ChangePasswordController : RestfulControllerBase<ChangePasswordController>
    {
        private readonly IChangePasswordService _changePasswordService;

        public ChangePasswordController(IChangePasswordService changePasswordService,
            ILogger<ChangePasswordController> logger) : base(logger)
        {
            _changePasswordService = changePasswordService;
        }

        [Produces(typeof(ChangePassResultVm))]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] ChangePasswordIm changePasswordIm)
        {
            try
            {
                var result = await _changePasswordService.ChangePassword(changePasswordIm);
                return Ok(result);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, e);
            }
        }
    }
}