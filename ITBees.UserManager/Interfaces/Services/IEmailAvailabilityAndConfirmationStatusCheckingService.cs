using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface IEmailAvailabilityAndConfirmationStatusCheckingService
    {
        CheckEmailStatusVm Check(string s, string email);
    }
}