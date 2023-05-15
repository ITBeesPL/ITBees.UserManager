using System.Threading.Tasks;
using ITBees.Models.Languages;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface IRoleAddingService
    {
        /// <summary>
        /// Use it with caution, You need to check if calling user has rights to promote other users
        /// </summary>
        /// <param name="email"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        Task AddRole(string email, string role, Language lang);
    }
}