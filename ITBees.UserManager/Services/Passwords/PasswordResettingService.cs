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
            var tokenAgeInfo = BuildTokenAgeInfo(passwordResetIm.IssuedAt);
            var user = await _userManager.FindByEmailAsync(passwordResetIm.Email);

            if (user == null)
            {
                _logger.LogWarning("Reset password failed - user {email} not found. {tokenAge}",
                    passwordResetIm.Email, tokenAgeInfo);
                throw new ArgumentException(Translate.Get(
                    () => Translations.UserManager.ResetPassword.EmailNotRegistered, new En()));
            }

            var token = DecodeResetToken(passwordResetIm.Token);
            var result = await _userManager.ResetPasswordAsync(user, token, passwordResetIm.NewPassword);
            if (result.Succeeded) return new ResetPassResultVm() { Success = true };

            var allErrorrs = string.Join(",", result.Errors.Select(x => x.Description));
            _logger.LogWarning(
                "Reset password failed for {email}. Errors: {errors}. {tokenAge}",
                passwordResetIm.Email, allErrorrs, tokenAgeInfo);

            return new ResetPassResultVm() { Success = false, Message = $"{Translate.Get(() => Translations.UserManager.ResetPassword.ResetPasswordErrorWithAdditionalMessages, new En())}{allErrorrs}" };
        }

        private static string DecodeResetToken(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput))
            {
                return rawInput;
            }

            try
            {
                var bytes = WebEncoders.Base64UrlDecode(rawInput);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return rawInput;
            }
        }

        private static string BuildTokenAgeInfo(long? issuedAtUnixSeconds)
        {
            if (!issuedAtUnixSeconds.HasValue)
            {
                return "IssuedAt=unknown";
            }

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnixSeconds.Value);
            var age = DateTimeOffset.UtcNow - issuedAt;
            return $"IssuedAt={issuedAt:O}, AgeHours={age.TotalHours:F1}";
        }

        public async Task<GenerateResetPasswordResultVm> GenerateResetPasswordLink(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("Reset password link request for unknown email {email}", email);
                throw new ArgumentException(Translate.Get(() => Translations.UserManager.ResetPassword.EmailNotRegistered, new En()));
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