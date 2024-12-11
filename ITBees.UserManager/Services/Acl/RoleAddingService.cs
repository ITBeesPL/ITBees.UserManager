using System;
using System.Threading.Tasks;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Translations;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Services.Acl
{
    public class RoleAddingService<T> : IRoleAddingService where T : IdentityUser<Guid>
    {
        private readonly IUserManager<T> _userManager;

        public RoleAddingService(IUserManager<T> userManager)
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