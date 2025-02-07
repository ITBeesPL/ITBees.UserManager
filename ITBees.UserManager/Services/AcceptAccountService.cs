using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Controllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services;

public class AcceptAccountService<T> : IAcceptAccountService<T> where T : IdentityUser<Guid>, new()
{
    private readonly ILoginService<T> _loginService;
    private readonly IUserManager<T> _userManager;

    public AcceptAccountService(ILoginService<T> loginService, IUserManager<T> userManager)
    {
        _loginService = loginService;
        _userManager = userManager;
    }

    public async Task<AcceptAccountResultVm> AcceptAccount(AcceptAccountIm x, string lang)
    {
        try
        {
            await _userManager.ResetPasswordAsync(x.Email, x.Token, x.NewPassword);
            await _loginService.ConfirmEmail(x.Email);
            var jwttoken = await _loginService.LoginAfterEmailConfirmation(x.Email, lang);
            return new AcceptAccountResultVm(true, jwttoken, null);
        }
        catch (Exception e)
        {
            throw new FasApiErrorException("Unable to set password, please contact with provider", 400);
        }
    }
}