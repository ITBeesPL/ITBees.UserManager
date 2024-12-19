using System;

namespace ITBees.UserManager.Controllers.Invitations.Models;

public class InvitationNewUserIm
{
    public Guid UserGuid { get; set; }
    public bool AcceptedInvitation { get; set; }
    public Guid CompanyGuid { get; set; }
    public string? Password { get; set; }
    public string ExternalAuthProviderName { get; set; }
}