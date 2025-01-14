using System;
using System.Linq;
using System.Threading.Tasks;
using ITBees.Interfaces.Platforms;
using ITBees.UserManager.Controllers.PlatformOperator;
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
    private readonly IPlatformSettingsService _platformSettingsService;

    public TokenAsService(IAspCurrentUserService aspCurrentUserService, 
        ILogger<ITokenAsService<T>> logger, 
        ILoginService<T> loginService,
        IPlatformSettingsService platformSettingsService)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _logger = logger;
        _loginService = loginService;
        _platformSettingsService = platformSettingsService;
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

    public async Task<LogiAsResultVm> GetLoginAsData(LoginAsIm loginAsIm)
    {
        var token = await GetToken(new TokenAsIm(){Email = loginAsIm.Email});
        var serviceUrl = _platformSettingsService.GetSetting(loginAsIm.ServiceType);
        return new LogiAsResultVm()
        {
            Token = token.Value,
            Url = $"{serviceUrl}/auth/astoken/{token.Value}"
        };
    }
}