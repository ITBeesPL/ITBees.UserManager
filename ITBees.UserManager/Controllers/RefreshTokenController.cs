using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Helpers;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITBees.UserManager.Controllers
{
    [CustomControllerName("RefreshToken")]
    [GenericRestControllerNameConvention]
    [Authorize]
    [Route("[controller]")]
    public class RefreshTokenController<T> : RestfulControllerBase<RefreshTokenController<T>> where T : IdentityUser<Guid>
    {
        private readonly ILoginService<T> _loginService;
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly RestrictedTokenOptions _restrictedTokenOptions;

        public RefreshTokenController(ILoginService<T> loginService,
            IAspCurrentUserService aspCurrentUserService,
            IOptions<RestrictedTokenOptions> restrictedTokenOptions,
            ILogger<RefreshTokenController<T>> logger) : base(logger)
        {
            _loginService = loginService;
            _aspCurrentUserService = aspCurrentUserService;
            _restrictedTokenOptions = restrictedTokenOptions.Value;
        }

        /// <summary>
        /// Answers with a fresh login token for the user of the token this request carries, so a client can
        /// extend its session before the token expires without asking for the password again. The body is
        /// empty. An expired token does not get past the authorization (401) - the user has to log in again.
        /// </summary>
        /// <returns>Returns jwt token and token expiration date, like POST /Login</returns>
        [HttpPost]
        [Produces(typeof(TokenVm))]
        public async Task<IActionResult> Post()
        {
            return await ReturnOkResultAsync(async () =>
            {
                // The answer is a regular login token of the token subject. A token an application
                // issued with narrowed-down rights must never be exchanged for one - otherwise whoever
                // holds it leaves with the full account.
                if (_restrictedTokenOptions.IsRestricted(User))
                {
                    throw new Authorization403ForbiddenException(
                        "This token is not allowed to be refreshed.");
                }

                var cu = _aspCurrentUserService.GetCurrentUser();
                if (cu == null)
                {
                    throw new UnauthorizedAccessException("this will be reported");
                }

                return await _loginService.RefreshToken(cu.Email, cu.Language.Code);
            });
        }
    }
}
