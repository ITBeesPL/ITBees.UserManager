using System;

namespace ITBees.UserManager.Services
{
    /// <summary>
    /// Represents result of new user creation
    /// </summary>
    public class NewUserRegistrationResult
    {
        public NewUserRegistrationResult(Guid userGuid, string errorMessages,
            Guid? invoiceDataGuid, Guid companyGuid)
        {
            UserGuid = userGuid;
            ErrorMessages = errorMessages;
            InvoiceDataGuid = invoiceDataGuid;
            CompanyGuid = companyGuid;
        }

        public Guid CompanyGuid { get; set; }

        public Guid? InvoiceDataGuid { get; set; }

        public Guid UserGuid { get; set; }
        public string ErrorMessages { get; }
    }
}