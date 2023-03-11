using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface ILoginService
    {
        Task<TokenVm> Login(string email, string pass);
    }
}