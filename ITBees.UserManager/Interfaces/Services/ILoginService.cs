using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface ILoginService<T> where T : IdentityUser
    {
        Task<TokenVm> Login(string email, string pass);
    }
}