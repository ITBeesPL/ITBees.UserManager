using System;

namespace ITBees.UserManager.Interfaces.Models
{
    public class NewUserRegistrationIm
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public Guid? CompanyGuid { get; set; }

        public string CompanyName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string Language { get; set; }
    }
}