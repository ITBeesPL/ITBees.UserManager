using System;

namespace ITBees.UserManager.Controllers.Models;

public class InvitationResendIm
{
    public Guid CompanyGuid { get; set; }
    public string UserEmail { get; set; }
}