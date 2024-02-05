using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.AppleLogins;
using ITBees.UserManager.Services.Registration;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Controllers
{
    public interface IAppleLoginService<T> where T : IdentityUser
    {
        Task<TokenVm> LoginOrRegister(AppleTokenResponse appleAuthorizationToken, string lang);
        Task<AppleTokenResponse> ValidateAuthorizationCodeAsync(string authorizationCode);
    }
}