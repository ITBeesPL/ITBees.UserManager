using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Google.Apis.Auth;
using ITBees.Interfaces.Platforms;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.AppleLogins;
using Environment = ITBees.Interfaces.Platforms.Environment;

namespace ITBees.UserManager.Controllers
{

    [CustomControllerName("AppleLogin")]
    [ApiController]
    [GenericRestControllerNameConvention]
    [Route("/AppleLogin")]
    public class AppleLoginController<T> : RestfulControllerBase<AppleLoginController<T>> where T : IdentityUser<Guid>, new()
    {
        private readonly IAppleLoginService<T> _appleLoginService;
        private readonly IPlatformSettingsService _platformSettingsService;

        public AppleLoginController(
            IAppleLoginService<T> appleLoginService,
            ILogger<AppleLoginController<T>> logger, 
            IPlatformSettingsService platformSettingsService) : base(logger)
        {
            _appleLoginService = appleLoginService;
            _platformSettingsService = platformSettingsService;
        }

        [Produces(typeof(TokenVm))]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AppleSignInIm im, [FromHeader(Name = "Accept-Language")] string acceptLanguage)
        {
            try
            {
                if (string.IsNullOrEmpty(im.RedirectURI) == false && _platformSettingsService.GetCurrentEnvironment() == Environment.Prod)
                {
                    return CreateBaseErrorResponse(
                        "Could not set redirect URI for apple login service in production environment. You must set this in config file.",
                        im);
                }

                AppleTokenResponse appleIdentityToken = await _appleLoginService.ValidateAuthorizationCodeAsync(im.AuthorizationCode, im.ClientId, im.RedirectURI);

                var lang = LanaguageParser.ParseAcceptLanguageHeader(acceptLanguage);

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
    }
}