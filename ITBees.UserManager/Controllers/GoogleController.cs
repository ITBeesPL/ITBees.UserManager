using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Google.Apis.Auth;
using ITBees.Models.Languages;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Services.Registration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;


namespace ITBees.UserManager.Controllers
{
    [CustomControllerName("GoogleLogin")]
    [ApiController]
    [GenericRestControllerNameConvention]
    [Route("/GoogleLogin")]
    public class GoogleLoginController<T> : RestfulControllerBase<GoogleLoginController<T>> where T : IdentityUser, new()
    {
        private readonly IGoogleLoginService<T> _googleLoginService;

        public GoogleLoginController(
            IGoogleLoginService<T> googleLoginService,
            ILogger<GoogleLoginController<T>> logger) : base(logger)
        {
            _googleLoginService = googleLoginService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GoogleSignInModel model, [FromHeader(Name = "Accept-Language")] string acceptLanguage)
        {
            string idToken = model.IdToken;

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

                if (string.IsNullOrEmpty(payload.Locale))
                {
                    payload.Locale = ParseAcceptLanguageHeader(acceptLanguage);
                }

                var googlePayload = new GooglePayload(payload);
                var result = await _googleLoginService.LoginOrRegister(googlePayload);
                return Ok(result);
            }
            catch (InvalidJwtException e)
            {
                return CreateBaseErrorResponse(e, model);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, model);
            }
        }

        private string ParseAcceptLanguageHeader(string acceptLanguage)
        {
            if (string.IsNullOrWhiteSpace(acceptLanguage))
            {
                return null;
            }

            string[] languages = acceptLanguage.Split(',');
            if (languages.Length > 0)
            {
                return languages[0].Split(';')[0].Substring(0,2);
            }

            return null;
        }
    }

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

    public class GoogleSignInModel
    {
        public string IdToken { get; set; }
    }
}