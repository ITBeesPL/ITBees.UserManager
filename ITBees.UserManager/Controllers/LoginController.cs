﻿using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [CustomControllerName("Login")]
    [ApiController]
    [GenericRestControllerNameConvention]
    [Route("/Login")]
    public class LoginController<T> : RestfulControllerBase<LoginController<T>> where T: IdentityUser
    {
        private readonly ILoginService<T> _loginService;

        public LoginController(ILoginService<T> loginService, ILogger<LoginController<T>> logger) : base(logger)
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
                    return base.CreateBaseErrorResponse(ModelState,string.Empty);
                }

                var token = await _loginService.Login(loginIm.Username, loginIm.Password, loginIm.Language);
                return Ok(token);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}