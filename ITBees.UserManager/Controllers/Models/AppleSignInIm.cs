namespace ITBees.UserManager.Controllers.Models
{
    public class AppleSignInIm
    {
        public string IdentityToken { get; set; }
        public string AuthorizationCode { get; set; }
        public string? User { get; set; }
        public string? ClientId { get; set; }
        public string? RedirectURI { get; set; }
    }
}