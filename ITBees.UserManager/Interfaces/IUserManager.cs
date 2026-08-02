using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces
{
    public interface IUserManager<T> where T : IdentityUser<Guid>
    {
        Task<IdentityResult> CreateAsync(object user, string password);

        Task<T> FindByEmailAsync(string email);

        Task<bool> CheckPasswordAsync(object user, string password);

        Task<string> GenerateEmailConfirmationTokenAsync(dynamic user);

        Task<IdentityResult> ConfirmEmailAsync(object user, string token);

        Task<IdentityResult> AddToRoleAsync(object user, string role);

        Task<IList<string>> GetRolesAsync(object user);

        Task<IdentityResult> ResetPasswordAsync(object user, string token, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(object user);
        Task<T> FindByIdAsync(string userIDGuid);
        Task<IdentityResult> ChangePasswordAsync(object user, string currentPass, string newPass);
        /// <summary>
        /// Deletes user account in database
        /// </summary>
        /// <param name="leaveAccountGuidForFutureBillingInformation">
        /// If set to true, it will only lockout account, and change email address by adding "_DELETED_yyyyMMddHHmm" at the end of user email ie : youremail@yourdomain.com_DELETED_202402011820, so the original address can be used for a new registration.
        /// </param>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        Task DeleteAccount(bool leaveAccountGuidForFutureBillingInformation, Guid userGuid);
    }
}