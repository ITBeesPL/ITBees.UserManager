using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface INewUserRegistrationService
    {
        Task<NewUserRegistrationResult> RegisterNewUser(NewUserRegistrationIm newUserRegistrationInputDto);
    }
}