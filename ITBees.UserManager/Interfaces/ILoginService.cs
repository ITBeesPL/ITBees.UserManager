using System.Threading.Tasks;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces
{
    public interface ILoginService<T> where T : IdentityUser
    {
        Task<TokenVm> Login(string email, string pass, string lang);
        Task<TokenVm> LoginAfterEmailConfirmation(string email, string lang);
        Task<ConfirmEmailResult> ConfirmEmail(string googlePayloadEmail);
        Task<MyAccountWithTokenVm> GetMyAccountWithTokenWithoutAuthorization(MyAccountVm email, string language);
    }
}