using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface IEmailAvailabilityAndConfirmationStatusCheckingService
    {
        Task<CheckEmailStatusVm> Check(string s, string email);
    }
}