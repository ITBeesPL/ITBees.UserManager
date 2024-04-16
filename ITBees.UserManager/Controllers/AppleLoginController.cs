using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Google.Apis.Auth;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.AppleLogins;

namespace ITBees.UserManager.Controllers
{
    
    [CustomControllerName("AppleLogin")]
    [ApiController]
    [GenericRestControllerNameConvention]
    [Route("/AppleLogin")]
    public class AppleLoginController<T> : RestfulControllerBase<AppleLoginController<T>> where T : IdentityUser, new()
    {
        private readonly IAppleLoginService<T> _appleLoginService;

        public AppleLoginController(
            IAppleLoginService<T> appleLoginService,
            ILogger<AppleLoginController<T>> logger) : base(logger)
        {
            _appleLoginService = appleLoginService;
        }

        [Produces(typeof(TokenVm))]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AppleSignInIm im, [FromHeader(Name = "Accept-Language")] string acceptLanguage)
        {
            try
            {
                AppleTokenResponse appleIdentityToken = await _appleLoginService.ValidateAuthorizationCodeAsync(im.AuthorizationCode, im.ClientId);

                var lang = ParseAcceptLanguageHeader(acceptLanguage);
                

                var result = await _appleLoginService.LoginOrRegister(appleIdentityToken, lang);

                return Ok(result);
            }
            catch (InvalidJwtException e)
            {
                return CreateBaseErrorResponse(e, im);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, im);
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
                return languages[0].Split(';')[0].Substring(0, 2);
            }

            return null;
        }
    }
}