using System.Linq;
using System.Threading.Tasks;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Registration
{
    public class GoogleLoginService<T> : IGoogleLoginService<T> where T :IdentityUser, new()
    {
        private readonly ILoginService<T> _loginService;
        private readonly INewUserRegistrationFromGoogle _newUserRegistrationFromGoogle;
        private readonly IReadOnlyRepository<UserAccount> _userOnlyRepository;

        public GoogleLoginService(ILoginService<T> loginService, 
            INewUserRegistrationFromGoogle newUserRegistrationFromGoogle,
            IReadOnlyRepository<UserAccount> userOnlyRepository)
        {
            _loginService = loginService;
            _newUserRegistrationFromGoogle = newUserRegistrationFromGoogle;
            _userOnlyRepository = userOnlyRepository;
        }
        public async Task<TokenVm> LoginOrRegister(GooglePayload googlePayload)
        {
            var userAccount = _userOnlyRepository.GetData(x => x.Email == googlePayload.Email).FirstOrDefault();
            if (userAccount == null)
            {
                var result =
                    await _newUserRegistrationFromGoogle.CreateNewUserAccountFromGoogleLogin(googlePayload,
                        googlePayload.Language.Code);
                return result;
            }

            return await _loginService.LoginAfterEmailConfirmation(userAccount.Email, googlePayload.Language.Code);
        }
    }
}