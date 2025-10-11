using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services
{
    public class ConfirmRegistrationService<T> : IConfirmRegistrationService<T> where T : IdentityUser<Guid>
    {
        private readonly IUserManager<T> _userManager;
        private readonly ILoginService<T> _loginService;
        private readonly ILogger<ConfirmRegistrationService<T>> _logger;

        public ConfirmRegistrationService(IUserManager<T> userManager, ILoginService<T> loginService,
            ILogger<ConfirmRegistrationService<T>> logger)
        {
            _userManager = userManager;
            _loginService = loginService;
            _logger = logger;
        }

        public async Task<TokenVm> ConfirmRegistrationEmailAndGetSessionToken(
            ConfirmRegistrationIm confirmRegistrationIm)
        {
            var user = await _userManager.FindByEmailAsync(confirmRegistrationIm.Email);
            if (user == null)
            {
                _logger.LogError($"User {confirmRegistrationIm.Email} not found");
                throw new Exception(
                    Translate.Get(() => Translations.UserManager.UserLogin.EmailNotRegistered, new En()));
            }

            var tokenBytes = WebEncoders.Base64UrlDecode(confirmRegistrationIm.Token);
            var token = Encoding.UTF8.GetString(tokenBytes);
            var confirmResult = await _userManager.ConfirmEmailAsync(user, token);
            if (confirmResult.Succeeded)
            {
                var result = await _loginService.LoginAfterEmailConfirmation(user.Email, "en");
                return result;
            }

            var errors = string.Join(";", confirmResult.Errors.Select(x => x.Description));
            
            _logger.LogError("Error on email confirmation for user {email} with token {token}. Errors: {errors}",
                confirmRegistrationIm.Email, confirmRegistrationIm.Token, errors);
            
            throw new Exception(Translate.Get(() => Translations.UserManager.UserLogin.ErrorOnConfirmationEmailAddress,
                                    new En()) +
                                $"Email :{confirmRegistrationIm.Email} token : {confirmRegistrationIm.Token} " + "(" +
                                errors + ")");
        }
    }
}