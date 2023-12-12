namespace ITBees.UserManager.Services.Passwords.Models
{
    public class PasswordResetIm
    {
        public string Email { get; set; }

        public string Token { get; set; }

        public string NewPassword { get; set; }
    }
}