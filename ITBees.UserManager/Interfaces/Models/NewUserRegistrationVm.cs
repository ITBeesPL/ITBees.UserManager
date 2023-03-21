using System;

namespace ITBees.UserManager.Interfaces.Models
{
    public class NewUserRegistrationVm
    {
        public Guid UserGuid { get; set; }
        public string ErrorMessages { get; set; }
    }
}