using ITBees.UserManager.Controllers;
using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces
{
    public interface IAcceptInvitationService
    {
        AcceptInvitationResultVm Accept(bool emailInvitation, string email, string company);
    }
}