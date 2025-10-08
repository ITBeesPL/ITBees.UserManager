using System.Threading.Tasks;
using ITBees.Models.Companies;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services;
using ITBees.UserManager.Services.Registration;

namespace ITBees.UserManager.Interfaces
{
    public interface INewUserRegistrationService<T> : INewUserRegistrationService where T : Company
    {
        
    }
    public interface INewUserRegistrationService
    {
        Task<NewUserRegistrationResult> CreateNewUser(
            NewUserRegistrationIm newUserRegistrationInputDto,
            bool sendConfirmationEmail = true, 
            AdditionalInvoiceDataIm additionalInvoiceDataIm = null,
            IInvitationEmailBodyCreator invitationEmailCreator = null,
            bool inviteToSetPassword = false,
            bool useTCompanyRepository = false);
        
        Task<NewUserRegistrationResult> CreateNewPartnerUser<T>(NewUserRegistrationIm newUserRegistrationInputDto,
            bool sendConfirmationAndInviteToSePasswordEmail, 
            IInvitationEmailBodyCreator invitationEmailCreator, 
            AdditionalInvoiceDataIm additionalInvoiceDataIm,
            bool inviteToSetPassword) where T : Company;

        Task<NewUserRegistrationResult> CreateAndInviteNewUserToCompany(
            NewUserRegistrationWithInvitationIm newUserRegistrationIm, 
            string accountEmailActivationBaseLink = "", 
            IExternalSecurityService externalSecurityService = null);

        Task ResendConfirmationEmail(string email);
        Task ResendInvitationToCompany(InvitationResendIm invitationIm);
    }
}