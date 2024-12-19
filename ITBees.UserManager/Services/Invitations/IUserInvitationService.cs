using ITBees.UserManager.Controllers.Invitations.Models;

namespace ITBees.UserManager.Services.Invitations;

public interface IUserInvitationService
{
    void ApplyNewUser(InvitationNewUserIm invitationNewUserIm);
    void ApplyExistingUser(InvitationExistingUserIm invitationExistingUserIm);
}