using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ITBees.BaseServices.Platforms.Interfaces;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services;

public class PlatformDebugLoginService<T> : LoginService<T> where T : IdentityUser<Guid>
{
    public PlatformDebugLoginService(
        IUserManager<T> userManager,
        IReadOnlyRepository<UserAccount> userReadOnlyRepository,
        IReadOnlyRepository<UsersInCompany> usersInCompanyReadOnlyRepository,
        IConfigurationRoot configurationRoot,
        IWriteOnlyRepository<UserAccount> userWriteOnlyRepository,
        ICurrentDateTimeService currentDateTimeService, ILogger<PlatformDebugLoginService<T>> logger) : base(
        userManager, userReadOnlyRepository, usersInCompanyReadOnlyRepository, configurationRoot,
        userWriteOnlyRepository, currentDateTimeService, logger)
    {
#if NOTDEBUG
       throw new Exception("You are not allowed to use Platform debug login service in production environment");
#endif
    }

    public override Task<TokenVm> Login(string email, string pass, string lang)
    {
        return base.LoginAfterEmailConfirmation(email, lang);
    }
}