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
        if (x == null)
            throw new FasApiErrorException("Invalid request", 400);

        x.Email = (x.Email ?? string.Empty).Trim();

        _logger.LogInformation("Accepting user account: {email}", x.Email);

        var user = await _userManager.FindByEmailAsync(x.Email);
        if (user == null)
        {
            _logger.LogError("AcceptAccount: user not found for email {email}", x.Email);
            throw new FasApiErrorException("Invalid email or token", 400);
        }

        // Clean possible whitespace/newlines that may appear after copy-paste or email clients wrapping.
        var tokenAuthB64 = (x.TokenAuth ?? string.Empty).Trim()
            .Replace(" ", "")
            .Replace("\r", "")
            .Replace("\n", "");

        if (string.IsNullOrEmpty(tokenAuthB64))
        {
            _logger.LogError("AcceptAccount: TokenAuth is empty for email {email}", x.Email);
            throw new FasApiErrorException("Invalid token", 400);
        }

        string resetToken;
        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(tokenAuthB64);
            resetToken = Encoding.UTF8.GetString(tokenBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AcceptAccount: TokenAuth Base64Url decode failed for email {email}", x.Email);
            throw new FasApiErrorException("Invalid token", 400);
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, x.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Code + ":" + e.Description));
            _logger.LogError("AcceptAccount: ResetPassword failed for {email}. Errors: {errors}", x.Email, errors);

            // Do not log full token in production; log only length + prefix for diagnostics.
            _logger.LogError("AcceptAccount: decoded reset token length {len}, prefix {prefix}",
                resetToken.Length,
                resetToken.Length > 10 ? resetToken.Substring(0, 10) : resetToken);

            throw new FasApiErrorException("Unable to set password, please contact with provider", 400);
        }

        await _loginService.ConfirmEmail(x.Email);
        _acceptInvitationService.AcceptAllAwaitingInvitations(x.Email);

        var jwttoken = await _loginService.LoginAfterEmailConfirmation(x.Email, x.Lang);
        _logger.LogInformation("User account accepted: {email} with no errors.", x.Email);

        return new AcceptAccountResultVm(true, jwttoken, null);
    }
    catch (FasApiErrorException)
    {
        throw;
    }
    catch (Exception e)
    {
        _logger.LogError(e, "AcceptAccount: unhandled error for email {email}", x?.Email);
        throw new FasApiErrorException("Unable to set password, please contact with provider", 400);
    }
}
}