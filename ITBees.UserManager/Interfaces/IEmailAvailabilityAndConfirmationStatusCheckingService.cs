using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces
{
    public interface IEmailAvailabilityAndConfirmationStatusCheckingService
    {
        Task<CheckEmailStatusVm> Check(string s, string email);
    }
}