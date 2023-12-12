using Google.Apis.Auth;
using ITBees.Models.Languages;

namespace ITBees.UserManager.Controllers.Models
{
    public class GooglePayload
    {
        public GooglePayload(GoogleJsonWebSignature.Payload payload)
        {
            var language = payload.Locale == null ? new En() : new InheritedMapper.DerivedAsTFromStringClassResolver<Language>().GetInstance(payload.Locale.Substring(0, 2));

            Email = payload.Email;
            FirstName = payload.GivenName;
            LastName = payload.FamilyName;
            Picture= payload.Picture;
            EmailVerified = payload.EmailVerified;
            Language = language;
        }
        public bool EmailVerified { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Picture { get; set; }
        public Language Language { get; set; }
    }
}