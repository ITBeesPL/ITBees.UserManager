using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Translations
{
    public class UserManager : ITranslate
    {
        public class UserLogin : ITranslate
        {
            public static readonly string IncorrectEmailOrPassword = "Incorrect email or password!";
            public static readonly string EmailNotConfirmed = "Email not confirmed!";
            public static readonly string EmailNotRegisterd = "Email not registered!";
            public static readonly string ErrorOnConfirmationEmailAddress = "Unfortunately there was an error while email confirmation ";
            public static readonly string ThereIsNoActiveAccountForAnEmailAddress = "There is no active account for an email address: ";
            public static readonly string PlatformInternalErrorMissingUserData = "Platform internal error - missing user data";
        }
        public class NewUserRegistration : ITranslate
        {
            public static  readonly string CouldNotFindCompanyWithSpecifiedGuid = "Could not find the company with the given ID to which you want to assign the user";

            /// <summary>
            /// Error message shown when user tries to add different user when he is not owner of this company
            /// </summary>
            public static readonly string ToAddNewUserYouMustBeCompanyOwner = "To add new user You must be company owner!";
            /// <summary>
            /// If user try's to register account for another person, and is not logged in, or his token expired
            /// </summary>
            public static readonly string IfYouWantToAddNewUserToCompany = "If You want to add new user to company, you have to be authenticated!";
            /// <summary>
            /// Default company name set if user has not set own company name (only simple account registration)
            /// </summary>
            public static readonly string DefaultPrivateCompanyName = "Private";

            public static readonly string EmailAlreadyRegistered = "Email already registered";

            public static readonly string ToInviteNewUserYouMustSpecifyTargetCompany = "To invite new user You must specify target company";

            public static readonly string YouMustBeLoggedInToAddNewUser = "You must be logged in to add new user";

            public static readonly string EmailAlreadyRegisteredButNotConfirmed = "Email already registered, but not confirmed";

            public class Errors : ITranslate
            {
                public static readonly string ErrorWhileRegisteringAUserAccount = "Error while registering a user account, please contact platform operator";
            }
        }
    }
}