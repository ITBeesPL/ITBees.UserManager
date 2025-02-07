using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;


[GenericRestControllerNameConvention]
[CustomControllerName("AcceptAccount")]
public class AcceptAccountController<T> : RestfulControllerBase<AcceptAccountController<T>> where T : IdentityUser<Guid>, new()
{
    private readonly IAcceptAccountService<T> _acceptAccountService;

    public AcceptAccountController(ILogger<AcceptAccountController<T>> logger,
        IAcceptAccountService<T> acceptAccountService) : base(logger)
    {
        _acceptAccountService = acceptAccountService;
    }

    [HttpPost]
    [Produces(typeof(AcceptAccountResultVm))]
    public Task<IActionResult> Post([FromBody] AcceptAccountIm acceptAccountIm,
        [FromHeader(Name = "Accept-Language")] string acceptLanguage)
    {
        var lang = LanaguageParser.ParseAcceptLanguageHeader(acceptLanguage);
        return ReturnOkResultAsync(async () =>
            await _acceptAccountService.AcceptAccount(acceptAccountIm ,lang));
    }
}

public class AcceptAccountIm
{
    public string Email { get; }
    public string Token { get; }
    public string NewPassword { get; }

    public AcceptAccountIm()
    {
        
    }

    public AcceptAccountIm(string email, string token, string newPassword)
    {
        Email = email;
        Token = token;
        NewPassword = newPassword;
    }
}