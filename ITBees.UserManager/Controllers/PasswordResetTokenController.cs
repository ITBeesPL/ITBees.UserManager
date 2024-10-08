﻿using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
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
        public async Task<IActionResult> Post([FromBody] PasswordResetIm passwordResetIm)
        {
            return await ReturnOkResultAsync(async () => await _passwordResettingService.ResetPassword(passwordResetIm));
        }

        [Produces(typeof(GenerateResetPasswordResultVm))]
        [HttpGet]
        public async Task<IActionResult> Get(string email)
        {
            return await ReturnOkResultAsync(async () => await _passwordResettingService.GenerateResetPasswordLink(email));
        }
    }
}