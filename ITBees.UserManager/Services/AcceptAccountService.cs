using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services;

public class AcceptAccountService<T> : IAcceptAccountService<T> where T : IdentityUser<Guid>, new()
{
    private readonly ILoginService<T> _loginService;
    private readonly IUserManager<T> _userManager;
    private readonly ILogger<AcceptAccountService<T>> _logger;
    private readonly IAcceptInvitationService _acceptInvitationService;

    public AcceptAccountService(ILoginService<T> loginService, IUserManager<T> userManager, ILogger<AcceptAccountService<T>> logger, IAcceptInvitationService acceptInvitationService)
    {
        _loginService = loginService;
        _userManager = userManager;
        _logger = logger;
        _acceptInvitationService = acceptInvitationService;
    }

    public async Task<AcceptAccountResultVm> AcceptAccount(AcceptAccountIm x)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(x.Email);
            var tokenDecodedFromBase64 = WebEncoders.Base64UrlDecode(x.TokenAuth!);
            var token = Encoding.UTF8.GetString(tokenDecodedFromBase64);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, x.NewPassword);
            if (!resetResult.Succeeded)
            {
                var errorDescriptions = resetResult.Errors.Select(e => e.Description);
                _logger.LogError($"Error while accpeting user account : {JsonSerializer.Serialize(x)}, errors :\r\n" +errorDescriptions.Aggregate((a, b) => $"{a}, {b}"));
                _logger.LogError("decoded token: " + token);

                throw new FasApiErrorException("Unable to set password, please contact with provider", 400);
            }

            await _loginService.ConfirmEmail(x.Email);
            _acceptInvitationService.AcceptAllAwaitingInvitations(x.Email);
            var jwttoken = await _loginService.LoginAfterEmailConfirmation(x.Email, x.Lang);
            return new AcceptAccountResultVm(true, jwttoken, null);
        }
        catch (Exception e)
        {
            throw new FasApiErrorException("Unable to set password, please contact with provider", 400);
        }
    }
}