using System;
using System.Text;
using System.Threading.Tasks;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Registration
{
    public class NewUserRegistrationFromGoogle<T> : INewUserRegistrationFromGoogle where T : IdentityUser, new()
    {
        private readonly INewUserRegistrationService _newUserRegistrationService;
        private readonly ILoginService<T> _loginService;

        public NewUserRegistrationFromGoogle(INewUserRegistrationService newUserRegistrationService, ILoginService<T> loginService)

        {
            _newUserRegistrationService = newUserRegistrationService;
            _loginService = loginService;
        }

        public async Task<TokenVm> CreateNewUserAccountFromGoogleLogin(GooglePayload googlePayload)
        {
            await _newUserRegistrationService.CreateNewUser(
                new NewUserRegistrationIm()
                {
                    CompanyName = string.Empty,
                    FirstName = googlePayload.FirstName,
                    LastName = googlePayload.LastName,
                    Email = googlePayload.Email,
                    Language = googlePayload.Language.Code,
                    Password = RandomPasswordGenerator.GenerateRandomPassword(30)
                }, false);
            var loginAfterEmailConfirmation = await _loginService.LoginAfterEmailConfirmation(googlePayload.Email);
            await _loginService.ConfirmEmail(googlePayload.Email);
            return loginAfterEmailConfirmation;
        }
    }
    public static class RandomPasswordGenerator
    {
        private static readonly Random _random = new Random();
        private const string Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^_";

        public static string GenerateRandomPassword(int length)
        {
            var password = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                char randomChar = Characters[_random.Next(Characters.Length)];
                password.Append(randomChar);
            }

            return password.ToString();
        }
    }
}