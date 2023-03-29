using ITBees.BaseServices.Settings.Interfaces;
using ITBees.Translations.Interfaces;

namespace ITBees.UserManager.Interfaces
{
    public interface IUserManagerSettings : IHasReplaceableUpperFields
    {
        /// <summary>
        /// Specify url address which will be used to create link used by newly registered users to confirm their email address.
        /// At this endpoint You should create logic responsible for creating post request to api with this values, and implement error handling logic.
        /// It should not be direct api endpoint, but link to site which will be responsible for display confirmation message to user and create post request to api endpoint.
        /// For example if Your site url is https://YourHiperSite.com, and You created page for email confirmation on with address like : https://YourHiperSite.com/app/emailConfirmation
        /// then proper setting for this filed will be : EMAIL_CONFIRMATION_URL = "https://YourHiperSite.com/app/emailConfirmation".
        /// </summary>
        public string EMAIL_CONFIRMATION_URL { get; }
        
        /// <summary>
        /// Used for welcom message like - "Welocme to our 'HiperSite' service" <== [[PLATFORM_NAME]] will be replaced with HiperSite value
        /// </summary>
        public string PLATFORM_NAME { get; }

        /// <summary>
        /// Enter Your site default landing page. For example : https://YourHiperSite.com
        /// </summary>
        public string SITE_URL { get; }

        /// <summary>
        /// Enter Your application default url, For example : https://YourHiperSite.com/app or https://app.YourHiperSite.com
        /// </summary>
        public string APPLICATION_SITE_URL { get; }
    }
}