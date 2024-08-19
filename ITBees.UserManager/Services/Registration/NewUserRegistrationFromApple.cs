using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Registration
{
    public class NewUserRegistrationFromApple<T> : INewUserRegistrationFromApple where T : IdentityUser, new()
    {
        private readonly INewUserRegistrationService _newUserRegistrationService;
        private readonly ILoginService<T> _loginService;

        public NewUserRegistrationFromApple(INewUserRegistrationService newUserRegistrationService,
            ILoginService<T> loginService)

        {
            _newUserRegistrationService = newUserRegistrationService;
            _loginService = loginService;
        }

        public async Task<TokenVm> CreateNewUserAccountFromAppleLogin(AppleTokenClaims appleTokenClaims)
        {
            await _newUserRegistrationService.CreateNewUser(
                new NewUserRegistrationIm()
                {
                    CompanyName = string.Empty,
                    FirstName = appleTokenClaims.FirstName,
                    LastName = appleTokenClaims.LastName,
                    Email = appleTokenClaims.Email,
                    Password = RandomPasswordGenerator.GenerateRandomPassword(30)
                }, false);
            var loginAfterEmailConfirmation = await _loginService.LoginAfterEmailConfirmation(appleTokenClaims.Email,"en");
            await _loginService.ConfirmEmail(appleTokenClaims.Email);
            return loginAfterEmailConfirmation;
        }
    }
}