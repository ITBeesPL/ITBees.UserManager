using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services
{
    public class FASUserManager<T> : IUserManager<T> where T : IdentityUser<Guid>
    {
        private readonly UserManager<T> _userManager;
        private readonly ILogger<FASUserManager<T>> _logger;
        private readonly IWriteOnlyRepository<UserAccount> _userAccountRwRepo;

        public FASUserManager(UserManager<T> userManager, 
            ILogger<FASUserManager<T>> logger, 
            IWriteOnlyRepository<UserAccount> userAccountRwRepo)
        {
            _userManager = userManager;
            _logger = logger;
            _userAccountRwRepo = userAccountRwRepo;
        }
        public async Task<IdentityResult> CreateAsync(object user, string password)
        {
            return await _userManager.CreateAsync((T)user, password);
        }

        public async Task<T> FindByEmailAsync(string email)
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

        public async Task<IdentityResult> ResetPasswordAsync(object user, string token, string newPassword)
        {
            return await _userManager.ResetPasswordAsync((T)user, token, newPassword);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(object user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync((T)user);
        }

        public async Task<T> FindByIdAsync(string userIDGuid)
        {
            return await _userManager.FindByIdAsync(userIDGuid);
        }

        public async Task<IdentityResult> ChangePasswordAsync(object user, string currentPass, string newPass)
        {
            return await _userManager.ChangePasswordAsync((T)user, currentPass, newPass);
        }

        public async Task DeleteAccount(bool leaveAccountGuidForFutureBillingInformation, Guid userGuid)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userGuid.ToString());
                
                if (user.Email.StartsWith("DELETED_"))
                    throw new Exception("Error while delete user account");

                if (leaveAccountGuidForFutureBillingInformation)
                {
                    var newEmail = $"DELETED_{DateTime.Now.ToString("yyyyMMddHHmm")}_{user.Email}";
                    _userAccountRwRepo.UpdateData(x => x.Email == user.Email, x =>
                    {
                        x.Email = newEmail;
                    });

                    var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                    await _userManager.ChangeEmailAsync(user, newEmail, token);
                    await _userManager.SetUserNameAsync(user, newEmail);
                    user.Email = newEmail;
                    await _userManager.SetLockoutEnabledAsync(user, true);

                    
                }
                else
                {
                    await _userManager.DeleteAsync(user);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Delete account error, exception : {e.Message}", e);
                throw new Exception($"Delete account error, exception ");
            }
        }
    }
}