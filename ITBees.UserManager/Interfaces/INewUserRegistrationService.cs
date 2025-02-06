using System.Threading.Tasks;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services;

namespace ITBees.UserManager.Interfaces
{
    public interface INewUserRegistrationService
    {
        Task<NewUserRegistrationResult> CreateNewUser(NewUserRegistrationIm newUserRegistrationInputDto,
            bool sendConfirmationEmail = true, AdditionalInvoiceDataIm additionalInvoiceDataIm = null,
            IInvitationEmailBodyCreator invitationEmailCreator = null);
        Task<NewUserRegistrationResult> CreateNewPartnerUser(NewUserRegistrationIm newUserRegistrationInputDto,
            bool sendConfirmationEmail, IInvitationEmailBodyCreator invitationEmailCreator, AdditionalInvoiceDataIm additionalInvoiceDataIm = null);

        Task<NewUserRegistrationResult> CreateAndInviteNewUserToCompany(
            NewUserRegistrationWithInvitationIm newUserRegistrationIm);

        Task ResendConfirmationEmail(string email);
        Task ResendInvitationToCompany(InvitationResendIm invitationIm);
    }
}