using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.UserManager.Controllers;
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

    public AcceptAccountService(ILoginService<T> loginService, IUserManager<T> userManager,
        ILogger<AcceptAccountService<T>> logger, IAcceptInvitationService acceptInvitationService)
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

            // Log incoming TokenAuth (Base64Url) safely
            var tokenAuthIncoming = x.TokenAuth ?? string.Empty;
            _logger.LogInformation(
                "AcceptAccount: TokenAuth received. b64Len={b64Len}, b64Sha={b64Sha}, b64Prefix={b64Prefix}",
                tokenAuthIncoming.Length,
                TokenLogHelper.Sha256(tokenAuthIncoming),
                TokenLogHelper.Prefix(tokenAuthIncoming)
            );

            // Clean possible whitespace/newlines that can appear after copy/paste or email wrapping
            var tokenAuthB64 = tokenAuthIncoming.Trim()
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");

            if (string.IsNullOrEmpty(tokenAuthB64))
            {
                _logger.LogError("AcceptAccount: TokenAuth is empty after cleanup for {email}", x.Email);
                throw new FasApiErrorException("Invalid token", 400);
            }

            string resetTokenRaw;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(tokenAuthB64);
                resetTokenRaw = Encoding.UTF8.GetString(tokenBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AcceptAccount: TokenAuth Base64Url decode failed for {email}", x.Email);
                throw new FasApiErrorException("Invalid token", 400);
            }

            // Log decoded reset token (raw) safely
            _logger.LogInformation(
                "AcceptAccount: decoded reset token. rawLen={rawLen}, rawSha={rawSha}, rawPrefix={rawPrefix}",
                resetTokenRaw.Length,
                TokenLogHelper.Sha256(resetTokenRaw),
                TokenLogHelper.Prefix(resetTokenRaw)
            );

            var resetResult = await _userManager.ResetPasswordAsync(user, resetTokenRaw, x.NewPassword);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Code + ":" + e.Description));
                _logger.LogError("AcceptAccount: ResetPassword failed for {email}. Errors: {errors}", x.Email, errors);

                // Optional: log request model WITHOUT password (avoid leaking)
                _logger.LogError("AcceptAccount request model (without password): {model}",
                    JsonSerializer.Serialize(new
                    {
                        Email = x.Email,
                        Token = x.Token,
                        TokenAuth = TokenLogHelper.Prefix(x.TokenAuth),
                        TokenAuthLen = (x.TokenAuth ?? string.Empty).Length,
                        Lang = x.Lang
                    }));

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