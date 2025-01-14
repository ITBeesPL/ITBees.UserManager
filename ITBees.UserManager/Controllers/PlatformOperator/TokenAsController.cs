using System;
using System.Threading.Tasks;
using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.PlatformOperator.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers.PlatformOperator;

[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(Roles = Role.PlatformOperator)]
[GenericRestControllerNameConvention]
[CustomControllerName("TokenAs")]
public class TokenAsController<T> : RestfulControllerBase<TokenAsController<T>> where T : IdentityUser<Guid>
{
    private readonly ITokenAsService<T> _tokenAsService;

    public TokenAsController(ILogger<TokenAsController<T>> logger, ITokenAsService<T> tokenAsService) : base(logger)
    {
        _tokenAsService = tokenAsService;
    }

    [HttpPost]
    [Produces(typeof(TokenVm))]
    public async Task<IActionResult> Post([FromBody] TokenAsIm tokenAsIm)
    {
        return await ReturnOkResultAsync(async () => await _tokenAsService.GetToken(tokenAsIm));
    }
}