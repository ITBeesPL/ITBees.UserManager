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
            accountEmailActivationBaseLink = InvitationPageUrl(accountEmailActivationBaseLink,
                AcceptInvitationExistingUserUrlSetting, "acceptInvitationExistingUser");
            
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
                    $"/acceptInvitation?emailInvitation=true&email={HttpUtility.UrlEncode(userSavedData.Email)}&companyGuid={userSavedData.CompanyGuid}&key={Guid.NewGuid()}&company={HttpUtility.UrlEncode(companyCompanyName)}" +
                    ConfirmationUrlParametersBuilder.Build(userSavedData.ConfirmationUrlParameters))
                ;
            ;

            return new EmailMessage()
            {
                Subject = translatedSubject,
                BodyHtml = translatedBody,
                Recipients = userSavedData.Email
            };
        }

        /// <summary>
        /// The platform settings (config.json) naming the pages behind the links of the invitation e-mails, used when
        /// the caller passes no accountEmailActivationBaseLink: the page where a new (or never confirmed) account sets
        /// its password, and the page where a registered user logs in and accepts the invitation.
        /// </summary>
        public const string AcceptInvitationUrlSetting = "AcceptInvitationUrl";
        public const string AcceptInvitationExistingUserUrlSetting = "AcceptInvitationExistingUserUrl";

        /// <summary>
        /// Where the link of an invitation e-mail points. The address the caller passes wins; without it the platform
        /// setting of that e-mail, else the fas-auth page under APPLICATION_SITE_URL (auth/acceptInvitation,
        /// auth/acceptInvitationExistingUser); with no site URL either, DefaultApiUrl as up to 8.0.413 - a link a
        /// click in the e-mail cannot accept, since GET /AcceptInvitation needs the token of the logged-in user.
        /// </summary>
        private string InvitationPageUrl(string accountEmailActivationBaseLink, string settingName, string fasAuthPage)
        {
            if (string.IsNullOrEmpty(accountEmailActivationBaseLink) == false)
                return accountEmailActivationBaseLink;

            var configured = _platformSettingsService.GetSetting(settingName);
            if (string.IsNullOrWhiteSpace(configured) == false)
                return configured.Trim();

            var applicationSiteUrl = _userManagerSettings.APPLICATION_SITE_URL;
            if (string.IsNullOrWhiteSpace(applicationSiteUrl) == false)
                return $"{applicationSiteUrl.Trim().TrimEnd('/')}/auth/{fasAuthPage}";

            return _platformSettingsService.GetSetting("DefaultApiUrl");
        }

        public EmailMessage ComposeEmailWithUserCreationAndInvitationToOrganization(
            NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName,
            string token, // email token (Base64Url)
            Language userLanguage,
            string accountEmailActivationBaseLink = "",
            string tokenPassword = "" // password reset token (Base64Url)
        )
        {
            companyCompanyName = userSavedData.InvitationToCompany ?? companyCompanyName;
            var invitorName = userSavedData.InvitationCreatorName ?? " ";
            accountEmailActivationBaseLink = InvitationPageUrl(accountEmailActivationBaseLink,
                AcceptInvitationUrlSetting, "acceptInvitation");

            var translatedSubject = Translate.Get(
                () => NewUserRegistrationEmail.ComposeEmailWithUserCreationAndInvitationToOrganizationSubject,
                userSavedData.Language);

            translatedSubject = translatedSubject
                .Replace("[[COMPANY_NAME]]", companyCompanyName)
                .Replace("[[INVITING_NAME]]", companyCompanyName);

            var translatedBodyHtml = Translate.Get(
                () => NewUserRegistrationEmail.ComposeEmailWithUserCreationAndInvitationToOrganizationBody,
                userSavedData.Language);

            var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // IMPORTANT: UrlEncode tokenAuth too
            var parameters =
                "?emailInvitation=true" +
                "&token=" + HttpUtility.UrlEncode(token) +
                "&email=" + HttpUtility.UrlEncode(userSavedData.Email) +
                "&tokenAuth=" + HttpUtility.UrlEncode(tokenPassword) +
                "&company=" + HttpUtility.UrlEncode(companyCompanyName) +
                "&issuedAt=" + issuedAt +
                ConfirmationUrlParametersBuilder.Build(userSavedData.ConfirmationUrlParameters);

            translatedBodyHtml = translatedBodyHtml
                .Replace("[[INVITING_NAME]]", invitorName)
                .Replace("[[COMPANY_NAME]]", companyCompanyName)
                .Replace("[[PLATFORM_NAME]]", _userManagerSettings.PLATFORM_NAME)
                .Replace("[[EMAIL_CONFIRMATION_URL]]", accountEmailActivationBaseLink)
                .Replace("[[CONFIRMATION_PARAMETERS]]", parameters);

            return new EmailMessage()
            {
                Subject = translatedSubject,
                BodyHtml = translatedBodyHtml,
                Recipients = userSavedData.Email
            };
        }

        public EmailMessage ComposeEmailConfirmation(NewUserRegistrationIm newUser, string token, string tokenPassword, string companyName)
        {
            var translatedSubject = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailConfirmationSubject, newUser.Language);
            var translatedBody = Translate.Get(() => NewUserRegistrationEmail.ComposeEmailConfirmationBody, newUser.Language);

            var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var transformedSubject = ReplaceableValues.Process(translatedSubject, _userManagerSettings);
            var transformedBody = ReplaceableValues.Process(
                translatedBody,
                _userManagerSettings,
                new ReplaceableField("CONFIRMATION_PARAMETERS",
                    $"?token={HttpUtility.UrlEncode(token)}&email={HttpUtility.UrlEncode(newUser.Email)}&tokenAuth={HttpUtility.UrlEncode(tokenPassword)}&company={HttpUtility.UrlEncode(companyName)}&issuedAt={issuedAt}" +
                    ConfirmationUrlParametersBuilder.Build(newUser.ConfirmationUrlParameters)));
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