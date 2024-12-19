using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Translations
{
    public class UserInvitation : ITranslate
    {
        public static readonly string ErrorWhileAcceptingUserInvitation =
            "There was an error while accepting user invitation, message :";

        public static readonly string InvitationAlreadyAccepted = "Invitation is already accepted";
        public static readonly string InvitationNotExists = "Invitation not exists";
        public static readonly string UserForSpecifiedEmailNotFound = "Specfied email not found";
    }
}