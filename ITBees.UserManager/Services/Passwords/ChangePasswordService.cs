using System;
using System.Linq;
using System.Threading.Tasks;
using ITBees.Models.Languages;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Passwords
{
    public class ChangePasswordService : IChangePasswordService
    {
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly IUserManager _userManager;
        private readonly ILogger<IPasswordResettingService> _logger;

        public ChangePasswordService(IAspCurrentUserService aspCurrentUserService, IUserManager userManager, ILogger<IPasswordResettingService> logger)
        {
            _aspCurrentUserService = aspCurrentUserService;
            _userManager = userManager;
            _logger = logger;
        }
        public async Task<ChangePassResultVm> ChangePassword(ChangePasswordIm changePasswordIm)
        {
            var currentUser = _aspCurrentUserService.GetCurrentUser();

            var user = await _userManager.FindByIdAsync(currentUser.Guid.ToString());

            if (user == null)
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.ResetPassword.CurrentUserNotExits, new En()));
            }

            if (await _userManager.CheckPasswordAsync(user, changePasswordIm.CurrentPass))
            {
                var result = await _userManager.ChangePasswordAsync(user, changePasswordIm.CurrentPass, changePasswordIm.NewPass);
                if (result.Succeeded)
                {
                    return new ChangePassResultVm() { Success = true };
                }
                else
                {
                    var errors = result.Errors.Select(e => e.Description);
                    var allErrors = String.Join("; ", errors);

                    return new ChangePassResultVm() { Success = false, Message = allErrors };
                }
            }
            else
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.ResetPassword.CurrentPasswordIsIncorrect, new En()));
            };
        }
    }
}