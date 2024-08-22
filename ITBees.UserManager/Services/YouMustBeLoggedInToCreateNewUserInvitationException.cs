using System;

namespace ITBees.UserManager.Services
{
    public class YouMustBeLoggedInToCreateNewUserInvitationException : Exception
    {
        public YouMustBeLoggedInToCreateNewUserInvitationException(string message) : base(message)
        {

        }
    }
}