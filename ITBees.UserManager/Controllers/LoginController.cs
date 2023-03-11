using System;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [Controller]
    [Route("[Controller]")]
    public class LoginController : RestfulControllerBase<LoginController>
    {
        private readonly ILoginService _loginService;

        public LoginController(ILoginService loginService, ILogger<LoginController> logger) : base(logger)
        {
            _loginService = loginService;
        }

        /// <summary>
        /// Allows the user to log in, by returning valid token with expiration date
        /// </summary>
        /// <param name="loginIm">Login input model</param>
        /// <returns>Returns jwt token and token expiration date</returns>
        [Produces(typeof(TokenVm))]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginIm loginIm)
        {
            try
            {
                if (!TryValidateModel(loginIm))
                {
                    //_logger.LogError(
                    //    $"Error thrown while running Post on LoginTokenController, inputModel validation failed. User email: {inputModel.Username}, message: {this.ModelState.Values.Select(x => x.Errors[0].ErrorMessage)}");
                    return BadRequest(this.ModelState.Values.Select(x => x.Errors[0].ErrorMessage));
                }

                var token = await _loginService.Login(loginIm.Username, loginIm.Password);
                return Ok(token);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}