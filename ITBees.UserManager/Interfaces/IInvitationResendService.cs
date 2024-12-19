using ITBees.UserManager.Controllers;
using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces;

public interface IInvitationResendService
{
    void Resend(InvitationResendIm invitationResendIm);
}