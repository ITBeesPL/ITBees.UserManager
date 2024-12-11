using System;
using System.Linq;
using System.Threading.Tasks;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services
{
    public class EmailAvailabilityAndConfirmationStatusCheckingService<T> : IEmailAvailabilityAndConfirmationStatusCheckingService where T : IdentityUser<Guid>
    {
        private readonly IReadOnlyRepository<UserAccount> _userAccountRoRepository;
        private readonly IUserManager<T> _userManager;

        public EmailAvailabilityAndConfirmationStatusCheckingService(IReadOnlyRepository<UserAccount> userAccountRoRepository,
            IUserManager<T> userManager)
        {
            _userAccountRoRepository = userAccountRoRepository;
            _userManager = userManager;
        }

        public async Task<CheckEmailStatusVm> Check(string email, string lang)
        {
            var firstOrDefault = _userAccountRoRepository.GetData(x => x.Email == email).FirstOrDefault();
            if (firstOrDefault == null)
            {
                return new CheckEmailStatusVm() { Email = email, CheckStatus = CheckStatus.EmailNotRegistered, EmailAllowedToRegister = true, Message = string.Empty};
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user.EmailConfirmed)
            {
                return new CheckEmailStatusVm() { Email = email, CheckStatus = CheckStatus.EmailAlreadyRegistered, EmailAllowedToRegister = false, Message = Translate.Get(() => ITBees.UserManager.Translations.UserManager.NewUserRegistration.EmailAlreadyRegistered, lang) };
            }

            return new CheckEmailStatusVm() { Email = email, CheckStatus = CheckStatus.EmailAlreadyRegisteredButNotConfirmed, EmailAllowedToRegister = false, Message = Translate.Get(() => ITBees.UserManager.Translations.UserManager.NewUserRegistration.EmailAlreadyRegisteredButNotConfirmed, lang) };
        }
    }
}