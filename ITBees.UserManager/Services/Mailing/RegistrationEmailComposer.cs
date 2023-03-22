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
            throw new NotImplementedException();
        }

        public EmailMessage ComposeEmailWithUserCreationAndInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string token)
        {
            throw new NotImplementedException();
        }

        public EmailMessage ComposeEmailConfirmation(NewUserRegistrationIm newUser, string token)
        {
            throw new NotImplementedException();
        }
    }
}