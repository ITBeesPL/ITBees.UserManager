using System.Threading.Tasks;
using ITBees.UserManager.Controllers;

namespace ITBees.UserManager.Interfaces
{
    public interface ILoginService
    {
        Task<TokenVm> Login(string email, string pass);
    }
}