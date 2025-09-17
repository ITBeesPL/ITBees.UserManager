using System;
using System.Collections.Generic;
using System.Web;
using ITBees.BaseServices.Settings.Models;
using ITBees.BaseServices.Settings.Services;
using ITBees.Interfaces.Platforms;
using ITBees.Mailing.Interfaces;
using ITBees.Models.EmailMessages;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Translations;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Mailing
{
    public class RegistrationEmailComposer : IRegistrationEmailComposer
    {
        private readonly IEmailSendingService _emailSendingService;
        private readonly ILogger<RegistrationEmailComposer> _logger;
        private readonly IUserManagerSettings _userManagerSettings;
        private readonly IPlatformSettingsService _platformSettingsService;

        private readonly Dictionary<string, string> _replaceableFields = new Dictionary<string, string>()
        {
            {"PLATFORM_NAME",""},
            {"EMAIL_CONFIRMATION_URL",""}
        };

        public RegistrationEmailComposer(IEmailSendingService emailSendingService,
            ILogger<RegistrationEmailComposer> logger,
            IUserManagerSettings userManagerSettings,
            IPlatformSettingsService platformSettingsService)
        {
            _emailSendingService = emailSendingService;
            _logger = logger;
            _userManagerSettings = userManagerSettings;
            _platformSettingsService = platformSettingsService;
        }

        public EmailMessage ComposeEmailWithInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string nameOfInviter, Language userLanguage,
            string accountEmailActivationBaseLink = "")
        {
            accountEmailActivationBaseLink = GetBaseUrl(accountEmailActivationBaseLink);
            
            var translatedSubject = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailWithInvitationToOrganizationSubject, userSavedData.Language);
            translatedSubject = translatedSubject
                .Replace("[[COMPANY_NAME]]", companyCompanyName)
                .Replace("[[INVITING_NAME]]", nameOfInviter)
                ;
            var translatedBody = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailWithInvitationToOrganizationBody, userSavedData.Language);
            translatedBody = translatedBody
                    .Replace("[[INVITING_NAME]]", nameOfInviter)
                .Replace("[[PLATFORM_NAME]]", _userManagerSettings.PLATFORM_NAME)
                .Replace("[[COMPANY_NAME]]", companyCompanyName)
                .Replace("[[EMAIL_CONFIRMATION_URL]]", accountEmailActivationBaseLink)
                .Replace("[[CONFIRMATION_PARAMETERS]]",
                    $"/acceptInvitation?emailInvitation=true&email={HttpUtility.UrlEncode(userSavedData.Email)}&companyGuid={userSavedData.CompanyGuid}&key={Guid.NewGuid()}")
                ;
            ;

            return new EmailMessage()
            {
                Subject = translatedSubject,
                BodyHtml = translatedBody,
                Recipients = userSavedData.Email
            };
        }

        private string GetBaseUrl(string accountEmailActivationBaseLink)
        {
            if(string.IsNullOrEmpty(accountEmailActivationBaseLink))
                return _platformSettingsService.GetSetting("DefaultApiUrl");
            
            return accountEmailActivationBaseLink;
        }

        public EmailMessage ComposeEmailWithUserCreationAndInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string token, Language userLanguage, string accountEmailActivationBaseLink = "")
        {
            companyCompanyName = userSavedData.InvitationToCompany ?? companyCompanyName;
            var invitorName = userSavedData.InvitationCreatorName ?? " ";
            accountEmailActivationBaseLink = GetBaseUrl(accountEmailActivationBaseLink);
            
            var translatedSubject = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailWithUserCreationAndInvitationToOrganizationSubject, userSavedData.Language);
            translatedSubject = translatedSubject
                .Replace("[[COMPANY_NAME]]", companyCompanyName)
                .Replace("[[INVITING_NAME]]", companyCompanyName)
                ;

            var translatedBodyHtml = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailWithUserCreationAndInvitationToOrganizationBody, userSavedData.Language); ;
            translatedBodyHtml = translatedBodyHtml
                .Replace("[[INVITING_NAME]]", invitorName)
                .Replace("[[COMPANY_NAME]]", companyCompanyName)
                .Replace("[[PLATFORM_NAME]]", _userManagerSettings.PLATFORM_NAME)
                .Replace("[[EMAIL_CONFIRMATION_URL]]", accountEmailActivationBaseLink)
                .Replace("[[CONFIRMATION_PARAMETERS]]",
                    $"?emailInvitation=true&token={HttpUtility.UrlEncode(token)}&email={HttpUtility.UrlEncode(userSavedData.Email)}")
                ;

            return new EmailMessage()
            {
                Subject = translatedSubject,
                BodyHtml = translatedBodyHtml,
                Recipients = userSavedData.Email
            };
        }

        public EmailMessage ComposeEmailConfirmation(NewUserRegistrationIm newUser, string token)
        {
            var translatedSubject = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailConfirmationSubject, newUser.Language);
            var translatedBody = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailConfirmationBody, newUser.Language);

            var transformedSubject = ReplaceableValues.Process(translatedSubject, _userManagerSettings);
            var transformedBody = ReplaceableValues.Process(
                translatedBody,
                _userManagerSettings,
                new ReplaceableField("CONFIRMATION_PARAMETERS",
                    $"?token={HttpUtility.UrlEncode(token)}&email={HttpUtility.UrlEncode(newUser.Email)}"));
            transformedBody = transformedBody.Replace("[[EMAIL_CONFIRMATION_URL]]", _userManagerSettings.EMAIL_CONFIRMATION_URL);

            return new EmailMessage()
            {
                Subject = transformedSubject,
                BodyHtml = transformedBody,
                Recipients = newUser.Email
            };
        }
    }
}