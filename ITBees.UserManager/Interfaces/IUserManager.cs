using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces
{
    public interface IUserManager
    {
        Task<IdentityResult> CreateAsync(object user, string password);

        Task<IdentityUser> FindByEmailAsync(string email);

        Task<bool> CheckPasswordAsync(object user, string password);

        Task<string> GenerateEmailConfirmationTokenAsync(dynamic user);

        Task<IdentityResult> ConfirmEmailAsync(object user, string token);

        Task<IdentityResult> AddToRoleAsync(object user, string role);

        Task<IList<string>> GetRolesAsync(object user);

        Task<IdentityResult> ResetPasswordAsync(object user, string token, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(object user);
        Task<IdentityUser> FindByIdAsync(string userIDGuid);
        Task<IdentityResult> ChangePasswordAsync(object user, string currentPass, string newPass);
        /// <summary>
        /// Deletes user account in database
        /// </summary>
        /// <param name="leaveAccountGuidForFutureBillingInformation">
        /// If set to true, it will only lockout account, and change email address by adding "DELETED_yyyyMMddHHmm_" to user email ie : DELETED_202402011820_youremail@yourdomain.com"
        /// </param>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        Task DeleteAccount(bool leaveAccountGuidForFutureBillingInformation, Guid userGuid);
    }
}