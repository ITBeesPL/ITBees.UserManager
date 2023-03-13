using System;
using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface INewUserRegistrationService
    {
        Task<Guid> RegisterNewUser(NewUserRegistrationIm newUserRegistrationInputDto);
    }
}