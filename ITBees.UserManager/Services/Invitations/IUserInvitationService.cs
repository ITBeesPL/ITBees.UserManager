using ITBees.UserManager.Controllers.Invitations.Models;

namespace ITBees.UserManager.Services.Invitations;

public interface IUserInvitationService
{
    ApplyInvitationResultForNewUserVm ApplyNewUser(InvitationNewUserIm invitationNewUserIm);
    ApplyInvitationResultForExistingUserVm ApplyExistingUser(InvitationExistingUserIm invitationExistingUserIm);
}