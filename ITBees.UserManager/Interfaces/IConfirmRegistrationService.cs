using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces
{
    public interface IConfirmRegistrationService<T> where T : IdentityUser
    {
        Task<TokenVm> ConfirmRegistrationEmailAndGetSessionToken(ConfirmRegistrationIm confirmRegistrationIm);
    }
}