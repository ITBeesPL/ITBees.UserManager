using System.Threading.Tasks;
using ITBees.UserManager.Controllers;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Registration
{
    public interface IGoogleLoginService<T> where T : IdentityUser, new()
    {
        Task<TokenVm> LoginOrRegister(GooglePayload googlePayload);
    }
}