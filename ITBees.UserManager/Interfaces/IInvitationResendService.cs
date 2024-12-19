using ITBees.UserManager.Controllers;
using ITBees.UserManager.Controllers.Invitations.Models;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Services.Invitations;

namespace ITBees.UserManager.Interfaces;

public interface IInvitationResendService
{
    InvtiationResendResultVm Resend(InvitationResendIm invitationResendIm);
}