namespace ITBees.UserManager.Controllers
{
    public class AppleSignInIm
    {
        public string IdentityToken { get; set; }
        public string AuthorizationCode { get; set; }
        public string? User { get; set; }
        public string? ClientId { get; set; }
    }
}