using System;
using System.Threading.Tasks;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Interfaces.Services;
using ITBees.UserManager.Translations;

namespace ITBees.UserManager.Services.Acl
{
    public class RoleAddingService : IRoleAddingService
    {
        private readonly IUserManager _userManager;

        public RoleAddingService(IUserManager userManager)
        {
            _userManager = userManager;
        }

        public async Task AddRole(string email, string role, Language lang)
        {
            try
            {
                var timUser = await _userManager.FindByEmailAsync(email);

                if (timUser == null)
                {
                    var message = Translate.Get(() => UserRoles.UserIsNotRegisteredYet, lang);
                    throw new ArgumentNullException(message);
                }

                await _userManager.AddToRoleAsync(timUser, role);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}