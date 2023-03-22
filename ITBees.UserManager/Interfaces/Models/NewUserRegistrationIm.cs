namespace ITBees.UserManager.Interfaces.Models
{
    public class NewUserRegistrationIm : IVmWithLanguageDefined
    {
        public string Email { get; set; }

        public string Password { get; set; }

        /// <summary>
        /// Optional company (contex) name, if not set default name will be used like "Private" defined in Translation, path to this value > Translations.UserManager.NewUserRegistration.DefaultPrivateCompanyName
        /// </summary>
        public string CompanyName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string Language { get; set; }
    }
}