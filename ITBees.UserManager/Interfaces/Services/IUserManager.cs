using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface IUserManager
    {
        Task<IdentityResult> CreateAsync(object user, string password);

        Task<IdentityUser> FindByEmailAsync(string email);

        Task<bool> CheckPasswordAsync(object user, string password);

        Task<string> GenerateEmailConfirmationTokenAsync(object user);

        Task<IdentityResult> ConfirmEmailAsync(object user, string token);

        Task<IdentityResult> AddToRoleAsync(object user, string role);

        Task<IList<string>> GetRolesAsync(object user);

    }
}