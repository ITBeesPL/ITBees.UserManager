using System;
using System.Threading.Tasks;
using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.PlatformOperator.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers.PlatformOperator;

[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(Roles = Role.PlatformOperator)]
[GenericRestControllerNameConvention]
[CustomControllerName("LoginAsParam")]
public class LoginAsParamController<T> : RestfulControllerBase<LoginAsParamController<T>> where T : IdentityUser<Guid>
{
    private readonly ITokenAsService<T> _tokenAsService;

    public LoginAsParamController(ILogger<LoginAsParamController<T>> logger, ITokenAsService<T> tokenAsService) : base(logger)
    {
        _tokenAsService = tokenAsService;
    }

    [HttpPost]
    [Produces(typeof(LogiAsResultVm))]
    public async Task<IActionResult> Post([FromBody] LoginAsIm loginAsIm)
    {
        return await ReturnOkResultAsync(async () => await _tokenAsService.GetLoginAsData(loginAsIm));
    }
}