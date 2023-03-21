using System.Linq;
using System.Threading.Tasks;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;

namespace ITBees.UserManager.Services
{
    public class EmailAvailabilityAndConfirmationStatusCheckingService : IEmailAvailabilityAndConfirmationStatusCheckingService
    {
        private readonly IReadOnlyRepository<UserAccount> _userAccountRoRepository;
        private readonly IUserManager _userManager;

        public EmailAvailabilityAndConfirmationStatusCheckingService(IReadOnlyRepository<UserAccount> userAccountRoRepository,
            IUserManager userManager)
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