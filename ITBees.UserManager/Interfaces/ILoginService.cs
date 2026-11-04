using System;
using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces
{
    public interface ILoginService<T> where T : IdentityUser<Guid>
    {
        Task<TokenVm> Login(string email, string pass, string lang);
        Task<TokenVm> LoginAfterEmailConfirmation(string email, string lang);

        /// <summary>
        /// Answers with a fresh login token for the account, so a client that still holds a valid token can
        /// extend its session without asking for the password again (POST /RefreshToken, and PUT /MyAccount after
        /// an account update). It is not a login: LoginsCount is left alone, only LastLoginDateTime moves. Like
        /// LoginAfterEmailConfirmation it issues a token without any check, so the caller has to make sure that
        /// the email is the one of the current user.
        /// </summary>
        Task<TokenVm> RefreshToken(string email, string lang);
        Task<ConfirmEmailResult> ConfirmEmail(string googlePayloadEmail);
        Task<MyAccountWithTokenVm> GetMyAccountWithTokenWithoutAuthorization(MyAccountVm email, string language);
    }
}