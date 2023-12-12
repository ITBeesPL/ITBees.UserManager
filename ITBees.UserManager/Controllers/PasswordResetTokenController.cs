using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Services.Passwords;
using ITBees.UserManager.Services.Passwords.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    public class PasswordResetTokenController : RestfulControllerBase<PasswordResetTokenController>
    {
        private readonly IPasswordResettingService _passwordResettingService;

        public PasswordResetTokenController(IPasswordResettingService passwordResettingService,
            ILogger<PasswordResetTokenController> logger) : base(logger)
        {
            _passwordResettingService = passwordResettingService;
        }

        [Produces(typeof(ResetPassResultVm))]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] PasswordResetIm passwordResetIm)
        {
            try
            {
                var result = await _passwordResettingService.ResetPassword(passwordResetIm);
                return Ok(result);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, e);
            }
        }


        [Produces(typeof(GenerateResetPasswordResultVm))]
        [HttpGet]
        public async Task<IActionResult> Get(string email)
        {
            try
            {
                await _passwordResettingService.GenerateResetPasswordLink(email);
                return Ok();
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(email, e);
            }
        }
    }
}