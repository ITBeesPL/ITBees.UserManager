using System;

namespace ITBees.UserManager.Interfaces.Models
{
    public class NewUserRegistrationWithInvitationIm : IVmWithLanguageDefined

    {
        public string Email { get; set; }

        public Guid? CompanyGuid { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string Language { get; set; }

        public Guid? UserRoleGuid { get; set; }
        public bool SendEmailInvitation { get; set; }
    }
}