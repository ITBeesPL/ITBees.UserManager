using System;
using System.Linq;
using System.Threading.Tasks;
using ITBees.UserManager.Controllers.PlatformOperator.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.PlatformOperator;

public class TokenAsService<T> : ITokenAsService<T> where T : IdentityUser<Guid>
{
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly ILogger<ITokenAsService<T>> _logger;
    private readonly ILoginService<T> _loginService;

    public TokenAsService(IAspCurrentUserService aspCurrentUserService, ILogger<ITokenAsService<T>> logger, ILoginService<T> loginService)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _logger = logger;
        _loginService = loginService;
    }

    public async Task<TokenVm> GetToken(TokenAsIm tokenAsIm)
    {
        var cu = _aspCurrentUserService.GetCurrentUser();
        _logger.LogCritical($"User {cu.Email} trying to login as : {tokenAsIm.Email}");
        if (cu.UserRoles.Contains("PlatformOperator"))
        {
            return await _loginService.LoginAfterEmailConfirmation(tokenAsIm.Email, "en");
        }

        throw new UnauthorizedAccessException("You are not allowed to use this method. This attempt will be reported.");
    }
}