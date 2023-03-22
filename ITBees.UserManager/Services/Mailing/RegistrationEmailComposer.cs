using System;
using ITBees.Mailing.Interfaces;
using ITBees.Models.EmailMessages;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Mailing
{
    public class RegistrationEmailComposer : IRegistrationEmailComposer
    {
        private readonly IEmailSendingService _emailSendingService;
        private readonly ILogger<RegistrationEmailComposer> _logger;

        public RegistrationEmailComposer(IEmailSendingService emailSendingService, 
            ILogger<RegistrationEmailComposer> logger)
        {
            _emailSendingService = emailSendingService;
            _logger = logger;
        }

        public EmailMessage ComposeEmailWithInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string nameOfInviter)
        {
            return new EmailMessage()
            {
                Subject = "You have been invited to company : {}",
                BodyHtml = "<h1>Test</h1>",
                BodyText = "Test",
                Recipients = userSavedData.Email
            };
        }

        public EmailMessage ComposeEmailWithUserCreationAndInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string token)
        {
            return new EmailMessage()
            {
                Subject = "You have been invited to company : {}",
                BodyHtml = "<h1>Test</h1>",
                BodyText = "Test",
                Recipients = userSavedData.Email
            };
        }

        public EmailMessage ComposeEmailConfirmation(NewUserRegistrationIm newUser, string token)
        {
            return new EmailMessage()
            {
                Subject = "You have create account",
                BodyHtml = "<h1>Test</h1>",
                BodyText = "Test",
                Recipients = newUser.Email
            };
        }
    }
}