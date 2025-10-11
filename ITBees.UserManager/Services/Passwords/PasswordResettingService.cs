using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services.Passwords.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Passwords
{
    public class PasswordResettingService<T> : IPasswordResettingService where T :  IdentityUser<Guid>
    {
        private readonly IUserManager<T> _userManager;
        private readonly ILogger<IPasswordResettingService> _logger;
        private readonly IResetPasswordEmailConstructorService _resetPasswordEmailConstructorService;


        public PasswordResettingService(IUserManager<T> userManager, ILogger<IPasswordResettingService> logger, IResetPasswordEmailConstructorService resetPasswordEmailConstructorService)
        {
            _userManager = userManager;
            _logger = logger;
            _resetPasswordEmailConstructorService = resetPasswordEmailConstructorService;
        }

        public async Task<ResetPassResultVm> ResetPassword(PasswordResetIm passwordResetIm)
        {
            var user = await _userManager.FindByEmailAsync(passwordResetIm.Email);

            if (user == null)
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.ResetPassword.EmailNotRegistered, new En()));
            }
            var tokenBytes = WebEncoders.Base64UrlDecode(passwordResetIm.Token);
            var token = Encoding.UTF8.GetString(tokenBytes);
            var result = await _userManager.ResetPasswordAsync(user, token, passwordResetIm.NewPassword);
            if (result.Succeeded) return new ResetPassResultVm() { Success = true };

            var allErrorrs = string.Join(",", result.Errors.Select(x => x.Description));
            _logger.LogError(
                $"Error while trying to reset password for email: {passwordResetIm.Email} exception:\r\n {allErrorrs}");

            return new ResetPassResultVm() { Success = false, Message = $"{Translate.Get(() => Translations.UserManager.ResetPassword.ResetPasswordErrorWithAdditionalMessages, new En())}{allErrorrs}" };
        }

        public async Task<GenerateResetPasswordResultVm> GenerateResetPasswordLink(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.ResetPassword.EmailNotRegistered, new En()));
            }

            var raw = await _userManager.GeneratePasswordResetTokenAsync(user);
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
            if (token == null)
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.ResetPassword.UnableToGenerateNewPasswordResetToken, new En()));
            }

            GenerateResetPasswordResultVm result = _resetPasswordEmailConstructorService.SendResetEmail(email, token);
            return result;
        }
    }
}