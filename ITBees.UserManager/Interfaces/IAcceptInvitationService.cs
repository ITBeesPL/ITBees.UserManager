using System;
using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces
{
    public interface IAcceptInvitationService
    {
        AcceptInvitationResultVm Accept(bool emailInvitation, string email, Guid company);
    }
}