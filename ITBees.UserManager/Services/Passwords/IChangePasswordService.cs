using System.Threading.Tasks;

namespace ITBees.UserManager.Services.Passwords
{
    public interface IChangePasswordService
    {
        Task<ChangePassResultVm> ChangePassword(ChangePasswordIm changePasswordIm);
    }
}