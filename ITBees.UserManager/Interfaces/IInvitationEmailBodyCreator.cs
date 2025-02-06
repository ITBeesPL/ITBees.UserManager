using ITBees.Models.EmailMessages;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces;

public interface IInvitationEmailBodyCreator
{
    EmailMessage Create(NewUserRegistrationIm newUserRegistrationIm, string emailConfirmationToken);
}