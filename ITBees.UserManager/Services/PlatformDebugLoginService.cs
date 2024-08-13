using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ITBees.BaseServices.Platforms.Interfaces;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace ITBees.UserManager.Services;

public class PlatformDebugLoginService<T> : LoginService<T> where T : IdentityUser
{
    public PlatformDebugLoginService(
        IUserManager userManager,
        IReadOnlyRepository<UserAccount> userReadOnlyRepository,
        IReadOnlyRepository<UsersInCompany> usersInCompanyReadOnlyRepository,
        IConfigurationRoot configurationRoot,
        IWriteOnlyRepository<UserAccount> userWriteOnlyRepository,
        ICurrentDateTimeService currentDateTimeService) : base(userManager, userReadOnlyRepository, usersInCompanyReadOnlyRepository, configurationRoot, userWriteOnlyRepository, currentDateTimeService)
    {
#if NOTDEBUG
       throw new Exception("You are not allowed to use Platform debug login service in production environment");
#endif
    }

    public override Task<TokenVm> Login(string email, string pass)
    {
        return base.LoginAfterEmailConfirmation(email);
    }
}