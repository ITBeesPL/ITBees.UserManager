using System;
using System.Threading.Tasks;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Registration
{
    public interface IGoogleLoginService<T> where T : IdentityUser<Guid>, new()
    {
        Task<TokenVm> LoginOrRegister(GooglePayload googlePayload);
    }
}