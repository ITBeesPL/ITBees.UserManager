using System;

namespace ITBees.UserManager.Controllers.Invitations.Models;

public class InvitationExistingUserIm
{
    public Guid UserGuid { get; set; }
    public bool AcceptedInvitation { get; set; }
    public Guid CompanyGuid { get; set; }
}