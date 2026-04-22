namespace ITBees.UserManager.Interfaces.Models
{
    public class ConfirmRegistrationIm
    {
        public string Token { get; set; }
        public string Email { get; set; }

        /// <summary>
        /// Unix timestamp (seconds since epoch, UTC) indicating when the confirmation token was generated.
        /// Optional - provided by links that include &issuedAt=... so server can diagnose token age
        /// when confirmation fails (expired vs genuinely invalid).
        /// </summary>
        public long? IssuedAt { get; set; }
    }
}