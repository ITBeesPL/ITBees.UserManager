using System.Threading.Tasks;
using ITBees.UserManager.Controllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Services.Registration
{
    public interface INewUserRegistrationFromGoogle
    {
        Task<TokenVm> CreateNewUserAccountFromGoogleLogin(GooglePayload googlePayload, string lang);
    }
}