using ITBees.UserManager.Services.Passwords.Models;

namespace ITBees.UserManager.Services.Passwords
{
    public interface IResetPasswordEmailConstructorService
    {
        GenerateResetPasswordResultVm SendResetEmail(string email, string token);
    }
}