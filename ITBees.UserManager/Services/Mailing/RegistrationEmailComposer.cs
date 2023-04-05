using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ITBees.BaseServices.Settings.Models;
using ITBees.BaseServices.Settings.Services;
using ITBees.Mailing.Interfaces;
using ITBees.Models.EmailMessages;
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
        private readonly Dictionary<string, string> _replaceableFields = new Dictionary<string, string>()
        {
            {"PLATFORM_NAME",""},
            {"EMAIL_CONFIRMATION_URL",""}
        };

        public RegistrationEmailComposer(IEmailSendingService emailSendingService,
            ILogger<RegistrationEmailComposer> logger, IUserManagerSettings userManagerSettings)
        {
            _emailSendingService = emailSendingService;
            _logger = logger;
            _userManagerSettings = userManagerSettings;
        }

        public EmailMessage ComposeEmailWithInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string nameOfInviter)
        {
            return new EmailMessage()
            {
                Subject = $"{nameOfInviter} has invited You to company : {companyCompanyName}",
                BodyHtml = "<h1>Accept invitation in Your panel}</h1>",
                BodyText = "Test",
                Recipients = userSavedData.Email
            };
        }

        public EmailMessage ComposeEmailWithUserCreationAndInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string token)
        {
            return new EmailMessage()
            {
                Subject = $"You have been invited to company : {companyCompanyName}",
                BodyHtml = $"<h1>Token : {token}</h1>",
                BodyText = "Test",
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

            return new EmailMessage()
            {
                Subject = transformedSubject,
                BodyHtml = transformedBody,
                Recipients = newUser.Email
            };
        }
    }
}