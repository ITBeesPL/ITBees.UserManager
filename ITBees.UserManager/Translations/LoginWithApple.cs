using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Translations
{
    public class LoginWithApple : ITranslate
    {
        public class Errors : ITranslate
        { 
            public static readonly string EmailNotConfirmed = "E-mail not confirmed! You must confirm e-mail in Apple to use this application";
        }
    }
}