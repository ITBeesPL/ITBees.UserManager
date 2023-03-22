﻿using ITBees.Models.EmailMessages;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Services.Mailing
{
    public interface IRegistrationEmailComposer
    {
        /// <summary>
        /// If user has already account registered on platform, generate email with invitation only.
        /// </summary>
        /// <param name="userSavedData"></param>
        /// <param name="companyCompanyName"></param>
        /// <returns></returns>
        EmailMessage ComposeEmailWithInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData,
            string companyCompanyName, string nameOfInviter);

        /// <summary>
        /// Create message with invitation to platform, and email activation link
        /// </summary>
        /// <param name="userSavedData"></param>
        /// <param name="companyCompanyName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        EmailMessage ComposeEmailWithUserCreationAndInvitationToOrganization(NewUserRegistrationWithInvitationIm userSavedData, 
            string companyCompanyName, 
            string token);

        /// <summary>
        /// Create message with email confirmation for single user
        /// </summary>
        /// <param name="newUser"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        EmailMessage ComposeEmailConfirmation(NewUserRegistrationIm newUser, string token);
    }
}