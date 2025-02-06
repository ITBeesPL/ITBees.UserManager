using System.Threading.Tasks;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services;

namespace ITBees.UserManager.Interfaces
{
    public interface INewUserRegistrationService
    {
        Task<NewUserRegistrationResult> CreateNewUser(
            NewUserRegistrationIm newUserRegistrationInputDto,
            bool sendConfirmationEmail = true, 
            AdditionalInvoiceDataIm additionalInvoiceDataIm = null,
            IInvitationEmailBodyCreator invitationEmailCreator = null,
            bool inviteToSetPassword = false);
        
        Task<NewUserRegistrationResult> CreateNewPartnerUser(NewUserRegistrationIm newUserRegistrationInputDto,
            bool sendConfirmationAndInviteToSePasswordEmail, 
            IInvitationEmailBodyCreator invitationEmailCreator, 
            AdditionalInvoiceDataIm additionalInvoiceDataIm,
            bool inviteToSetPassword);

        Task<NewUserRegistrationResult> CreateAndInviteNewUserToCompany(
            NewUserRegistrationWithInvitationIm newUserRegistrationIm);

        Task ResendConfirmationEmail(string email);
        Task ResendInvitationToCompany(InvitationResendIm invitationIm);
    }
}