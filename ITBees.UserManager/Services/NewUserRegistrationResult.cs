using System;

namespace ITBees.UserManager.Services
{
    public class NewUserRegistrationResult
    {
        public NewUserRegistrationResult(Guid userGuid, string errorMessages)
        {
            UserGuid = userGuid;
            ErrorMessages = errorMessages;
        }
        public Guid UserGuid { get; set; }
        public string ErrorMessages { get; }
    }
}