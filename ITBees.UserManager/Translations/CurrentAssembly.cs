using System.Collections.Generic;
using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Translations
{
    public class CurrentAssembly
    {
        public static List<ITranslate> GetTranslationClasses()
        {
            return new List<ITranslate>() { new UserManager() , new NewUserRegistrationEmail(), new UserRoles(), new LoginWithApple()};
        }
    }
}