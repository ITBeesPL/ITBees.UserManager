using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Translations
{
    public class LoginWithFacebook : ITranslate
    {
        public class Errors : ITranslate
        {
            public static readonly string EmailCouldNotBeEmpty = "E-mail address could not be empty. Unable to login with facebook.";
        }
    }
}