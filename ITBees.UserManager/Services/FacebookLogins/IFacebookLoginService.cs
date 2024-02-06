using System.Threading.Tasks;
using ITBees.UserManager.Controllers;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.FacebookLogins
{
    public interface IFacebookLoginService<T> where T : IdentityUser
    {
        Task<FacebookLoginResult> ValidateAccessToken(string accessToken);
        Task<TokenVm> LoginOrRegister(FacebookLoginResult facebookLoginResult, string lang);
    }
}