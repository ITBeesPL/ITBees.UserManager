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
            var tokenAgeInfo = BuildTokenAgeInfo(confirmRegistrationIm.IssuedAt);

            var user = await _userManager.FindByEmailAsync(confirmRegistrationIm.Email);
            if (user == null)
            {
                _logger.LogWarning("Email confirmation failed - user {email} not found. {tokenAge}",
                    confirmRegistrationIm.Email, tokenAgeInfo);
                throw new ArgumentException(
                    Translate.Get(() => Translations.UserManager.UserLogin.EmailNotRegistered, new En()));
            }

            var token = DecodeConfirmationToken(confirmRegistrationIm.Token);
            var confirmResult = await _userManager.ConfirmEmailAsync(user, token);
            if (confirmResult.Succeeded)
            {
                var result = await _loginService.LoginAfterEmailConfirmation(user.Email, "en");
                return result;
            }

            var errors = string.Join(";", confirmResult.Errors.Select(x => x.Description));

            _logger.LogWarning(
                "Email confirmation failed for {email}. Errors: {errors}. {tokenAge}",
                confirmRegistrationIm.Email, errors, tokenAgeInfo);

            throw new ArgumentException(
                Translate.Get(() => Translations.UserManager.UserLogin.ErrorOnConfirmationEmailAddress, new En()) +
                $"Email :{confirmRegistrationIm.Email} (" + errors + ")");
        }

        private static string DecodeConfirmationToken(string rawInput)
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
                // Client sent raw identity token (unwrapped base64, with +/= chars) - use as-is.
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
    }
}