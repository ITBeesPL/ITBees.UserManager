using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;

namespace ITBees.UserManager.Services
{
    public class EmailAvailabilityAndConfirmationStatusCheckingService : IEmailAvailabilityAndConfirmationStatusCheckingService
    {
        private readonly IReadOnlyRepository<UserAccount> _userAccountRoRepository;

        public EmailAvailabilityAndConfirmationStatusCheckingService(IReadOnlyRepository<UserAccount> userAccountRoRepository)
        {
            _userAccountRoRepository = userAccountRoRepository;
        }

        public CheckEmailStatusVm Check(string email, string lang)
        {
            var firstOrDefault = _userAccountRoRepository.GetData(x => x.Email == email).FirstOrDefault();
            if (firstOrDefault == null)
            {
                return new CheckEmailStatusVm() { Email = email, CheckStatus = "Email is not registerd", EmailAllowedToRegister = true, Message = string.Empty};
            }
            return new CheckEmailStatusVm() { Email = email, CheckStatus = "Email already registerd", EmailAllowedToRegister = false, Message = "Email already registered on platform"};
        }
    }
}