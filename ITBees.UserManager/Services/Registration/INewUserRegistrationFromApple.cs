using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Services.Registration
{
    public interface INewUserRegistrationFromApple
    {
        Task<TokenVm> CreateNewUserAccountFromAppleLogin(ApplePayload applePayload);
    }
}