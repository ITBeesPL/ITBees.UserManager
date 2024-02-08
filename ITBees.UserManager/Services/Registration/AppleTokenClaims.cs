namespace ITBees.UserManager.Services.Registration
{
    public class AppleTokenClaims
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public long ExpirationTime { get; set; }
        public long IssuedAt { get; set; }
        public string Subject { get; set; }
        public string AccessTokenHash { get; set; }
        public string Email { get; set; }
        public bool EmailVerified { get; set; }
        public long AuthenticationTime { get; set; }
        public bool NonceSupported { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}