using System.Threading.Tasks;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Services.Passwords.Models;

namespace ITBees.UserManager.Services.Passwords
{
    public interface IPasswordResettingService
    {
        Task<ResetPassResultVm> ResetPassword(PasswordResetIm passwordResetIm);
        Task<GenerateResetPasswordResultVm> GenerateResetPasswordLink(string email);
    }
}