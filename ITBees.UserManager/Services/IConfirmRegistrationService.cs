using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services
{
    public interface IConfirmRegistrationService<T> where T : IdentityUser
    {
        Task<TokenVm> ConfirmRegistrationEmailAndGetSessinToken(ConfirmRegistrationIm confirmRegistrationIm);
    }
}