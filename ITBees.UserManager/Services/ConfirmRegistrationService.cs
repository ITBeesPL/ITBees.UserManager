﻿using System;
using System.Linq;
using System.Threading.Tasks;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services
{
    public class ConfirmRegistrationService<T> : IConfirmRegistrationService<T> where T : IdentityUser<Guid> 
    {
        private readonly IUserManager<T> _userManager;
        private readonly ILoginService<T> _loginService;

        public ConfirmRegistrationService(IUserManager<T> userManager, ILoginService<T> loginService)
        {
            _userManager = userManager;
            _loginService = loginService;
        }

        public async Task<TokenVm> ConfirmRegistrationEmailAndGetSessionToken(
            ConfirmRegistrationIm confirmRegistrationIm)
        {
            var user = await _userManager.FindByEmailAsync(confirmRegistrationIm.Email);
            if (user == null)
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.UserLogin.EmailNotRegistered, new En()));
            }
            var confirmResult = await _userManager.ConfirmEmailAsync(user, confirmRegistrationIm.Token);
            if (confirmResult.Succeeded)
            {
                var result = await _loginService.LoginAfterEmailConfirmation(user.Email, "en");
                return result;
            }

            var errors = string.Join(";", confirmResult.Errors.Select(x=>x.Description));
            throw new Exception(Translate.Get(() => Translations.UserManager.UserLogin.ErrorOnConfirmationEmailAddress, new En() + $"Email :{confirmRegistrationIm.Email} token : {confirmRegistrationIm.Token} ")+ "("+ errors +")");
        }
    }
}