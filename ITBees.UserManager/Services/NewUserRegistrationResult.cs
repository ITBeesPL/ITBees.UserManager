using System;

namespace ITBees.UserManager.Services
{
    /// <summary>
    /// Represents result of new user creation
    /// </summary>
    public class NewUserRegistrationResult
    {
        public NewUserRegistrationResult(Guid userGuid, string errorMessages,
            Guid? invoiceDataGuid)
        {
            UserGuid = userGuid;
            ErrorMessages = errorMessages;
            InvoiceDataGuid = invoiceDataGuid;
        }

        public Guid? InvoiceDataGuid { get; set; }

        public Guid UserGuid { get; set; }
        public string ErrorMessages { get; }
    }
}