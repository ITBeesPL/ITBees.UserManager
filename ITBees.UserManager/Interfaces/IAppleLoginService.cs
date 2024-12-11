using System;
using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.AppleLogins;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces
{
    public interface IAppleLoginService<T> where T : IdentityUser<Guid>
    {
        Task<TokenVm> LoginOrRegister(AppleTokenResponse appleAuthorizationToken, string lang);
        Task<AppleTokenResponse> ValidateAuthorizationCodeAsync(string authorizationCode, string clientId, string redirectURI);
    }
}