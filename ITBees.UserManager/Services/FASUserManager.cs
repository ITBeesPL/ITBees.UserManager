using System.Collections.Generic;
using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services
{
    public class FASUserManager<T> : IUserManager where T : IdentityUser
    {
        private readonly UserManager<T> _userManager;

        public FASUserManager(UserManager<T> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IdentityResult> CreateAsync(object user, string password)
        {
            return await _userManager.CreateAsync((T)user, password);
        }

        public async Task<IdentityUser> FindByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> CheckPasswordAsync(object user, string password)
        {
            return await _userManager.CheckPasswordAsync((T)user, password);
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(object user)
        {
            return await _userManager.GenerateEmailConfirmationTokenAsync((T)user);
        }

        public async Task<IdentityResult> ConfirmEmailAsync(object user, string token)
        {
            return await _userManager.ConfirmEmailAsync((T)user, token);
        }

        public async Task<IdentityResult> AddToRoleAsync(object user, string role)
        {
            return await _userManager.AddToRoleAsync((T)user, role);
        }

        public async Task<IList<string>> GetRolesAsync(object user)
        {
            return await _userManager.GetRolesAsync((T)user);
        }
    }
}