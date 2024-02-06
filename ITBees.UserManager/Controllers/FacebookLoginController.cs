using System;
using System.Threading.Tasks;
using Google.Apis.Auth;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.FacebookLogins;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [CustomControllerName("FacebookLogin")]
    [ApiController]
    [GenericRestControllerNameConvention]
    [Route("/FacebookLogin")]
    public class FacebookLoginController<T> : RestfulControllerBase<FacebookLoginController<T>> where T : IdentityUser, new()
    {
        private readonly IFacebookLoginService<T> _facebookLoginService;

        public FacebookLoginController(
            IFacebookLoginService<T> facebookLoginService,
            ILogger<FacebookLoginController<T>> logger) : base(logger)
        {
            _facebookLoginService = facebookLoginService;
        }

        [Produces(typeof(TokenVm))]
        [HttpPost]
        public async Task<IActionResult> Post(string accessToken, [FromHeader(Name = "Accept-Language")] string acceptLanguage)
        {
            try
            {
                FacebookLoginResult facebookLoginResult = await _facebookLoginService.ValidateAccessToken(accessToken);
                var lang = ParseAcceptLanguageHeader(acceptLanguage);
                
                var result = await _facebookLoginService.LoginOrRegister(facebookLoginResult, lang);

                return Ok(result);
            }
            catch (InvalidJwtException e)
            {
                return CreateBaseErrorResponse(e, accessToken);
            }
            catch (Exception e)
            {
                return CreateBaseErrorResponse(e, accessToken);
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