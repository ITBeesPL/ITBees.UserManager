using System;
using ITBees.UserManager.Services.Passwords.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Web;
using ITBees.Interfaces.Platforms;
using ITBees.Mailing.Interfaces;
using ITBees.Models.EmailMessages;
using ITBees.Translations;
using ITBees.Models.Languages;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;

namespace ITBees.UserManager.Services.Passwords
{
    public class ResetPasswordEmailConstructorService : IResetPasswordEmailConstructorService
    {
        private readonly ILogger<ResetPasswordEmailConstructorService> _logger;
        private readonly IEmailSendingService _emailSendingService;
        private readonly IReadOnlyRepository<UserAccount> _userAccountRoRepository;
        private readonly IUserManagerSettings _userManagerSettings;
        private readonly IPlatformSettingsService _platformSettingsService;

        public ResetPasswordEmailConstructorService(ILogger<ResetPasswordEmailConstructorService> logger,
            IEmailSendingService emailSendingService,
            IReadOnlyRepository<UserAccount> userAccountRoRepository,
            IUserManagerSettings userManagerSettings,
            IPlatformSettingsService platformSettingsService)
        {
            _logger = logger;
            _emailSendingService = emailSendingService;
            _userAccountRoRepository = userAccountRoRepository;
            _userManagerSettings = userManagerSettings;
            _platformSettingsService = platformSettingsService;
        }

        public GenerateResetPasswordResultVm SendResetEmail(string email, string token)
        {
            Language userLanguage = _userAccountRoRepository.GetData(x => x.Email == email, x => x.Language).First().Language;
            try
            {
                EmailMessage message = GetResetPasswordMessage(email, token, userLanguage);
                var platformEmailAccount = _platformSettingsService.GetPlatformDefaultEmailAccount();
                _emailSendingService.SendEmail(platformEmailAccount, message);
                return new GenerateResetPasswordResultVm { Success = true, Message = ""};
            }
            catch (Exception e)
            {
                var message =
                    Translate.Get(() => Translations.UserManager.ResetPassword.ErrorWhileTryingToResetPassword,
                        userLanguage);

                return new GenerateResetPasswordResultVm() { Success = false, Message = $"{message}{e.Message}" };
            }
        }

        private EmailMessage GetResetPasswordMessage(string email, string token, Language userLanguage)
        {
            var emailBody = Translate.Get(() => Translations.UserManager.ResetPassword.DefaultEmailBodyForPasswordResetStarted, userLanguage);
            var resetUrl = $"{_userManagerSettings.APPLICATION_SITE_URL}auth/reset?email={HttpUtility.UrlEncode(email)}&token={HttpUtility.UrlEncode(token)}";
            emailBody = emailBody.Replace("[[resetUrl]]", resetUrl).Replace("[[site.Url]]", _userManagerSettings.SITE_URL);
            return new EmailMessage()
            {
                Subject = $"{(Translate.Get(() => Translations.UserManager.ResetPassword.DefaultEmailSubjectForPasswordResetStarted, userLanguage))}",
                BodyHtml = emailBody,
                BodyText = string.Empty,
                Recipients = email
            };
        }
    }
}