namespace ITBees.UserManager.Services.Passwords.Models
{
    public class PasswordResetIm
    {
        public string Email { get; set; }

        public string Token { get; set; }

        public string NewPassword { get; set; }

        /// <summary>
        /// Unix timestamp (seconds since epoch, UTC) indicating when the reset-password token was generated.
        /// Optional - provided by links that include &issuedAt=... so server can diagnose token age on failure.
        /// </summary>
        public long? IssuedAt { get; set; }
    }
}