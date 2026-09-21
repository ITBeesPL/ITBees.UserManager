using System;
using System.Linq;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Interfaces.Repository;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.UserManager.Interfaces;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services;

public class MyAccountUpdateService : IMyAccountUpdateService
{
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IWriteOnlyRepository<UserAccount> _userAccountRwRepo;
    private readonly ILogger<MyAccountUpdateService> _logger;
    private readonly IReadOnlyRepository<Language> _langRoRepo;
    private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;

    public MyAccountUpdateService(IAspCurrentUserService aspCurrentUserService,
        IWriteOnlyRepository<UserAccount> userAccountRwRepo,
        ILogger<MyAccountUpdateService> logger,
        IReadOnlyRepository<Language> langRoRepo,
        IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _userAccountRwRepo = userAccountRwRepo;
        _logger = logger;
        _langRoRepo = langRoRepo;
        _usersInCompanyRoRepo = usersInCompanyRoRepo;
    }


    public void UpdateMyAccount(MyAccountIm myAccountIm)
    {
        var cu = _aspCurrentUserService.GetCurrentUser();
        if (cu == null)
        {
            throw new UnauthorizedAccessException("You must be logged in!");
        }

        // Checked before anything is saved; null leaves the stored company as it is.
        var lastUsedCompanyGuid = ResolveRequestedCompany(cu, myAccountIm.LastUsedCompanyGuid);

        try
        {
            string langTokenCode = cu.Language.Code;
            int? langId = null;

            if (string.IsNullOrEmpty(myAccountIm.Language) == false && langTokenCode != myAccountIm.Language)
            {
                if (myAccountIm.Language == "ua") //dirty hack for compability, ukrainian should be set as 'uk'
                {
                    myAccountIm.Language = "uk";
                }

                langId = _langRoRepo.GetData(x => x.Code == myAccountIm.Language).FirstOrDefault()!.Id;
                var langUpdateResult = _userAccountRwRepo.UpdateData(x => x.Guid == cu.Guid, x =>
                {
                    x.LanguageId = langId.Value;
                    x.FirstName = myAccountIm.FirstName;
                    x.LastName = myAccountIm.LastName;
                    x.Phone = myAccountIm.Phone;
                    if (lastUsedCompanyGuid.HasValue)
                        x.LastUsedCompanyGuid = lastUsedCompanyGuid.Value;
                }, x => x.Language, x => x.UserAccountModules);
                if (langUpdateResult.Count == 0)
                {
                    throw new Exception("Didn't update my account, record not found");
                }
            }
            else
            {
                var userAccount = _userAccountRwRepo.UpdateData(x => x.Guid == cu.Guid, x =>
                {
                    x.FirstName = myAccountIm.FirstName;
                    x.LastName = myAccountIm.LastName;
                    x.Phone = myAccountIm.Phone;                    
                    if (lastUsedCompanyGuid.HasValue)
                        x.LastUsedCompanyGuid = lastUsedCompanyGuid.Value;
                }, x=>x.Language, x=>x.UserAccountModules);
                if (userAccount.Count == 0)
                {
                    throw new Exception("Didn't update my account language, record not found");
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("UpdateMyAccount error : " + e.Message, e);
            throw;
        }
    }

    /// <summary>
    /// LastUsedCompanyGuid goes into the login token and applications scope their data by it, so an
    /// account may only be pointed at a company its user belongs to - the request alone proves
    /// nothing. An empty guid (a client that does not send the field) leaves the company unchanged.
    /// </summary>
    private Guid? ResolveRequestedCompany(CurrentUser cu, Guid requestedCompanyGuid)
    {
        if (requestedCompanyGuid == Guid.Empty)
        {
            return null;
        }

        var belongsToCompany = _usersInCompanyRoRepo.HasData(x =>
            x.UserAccountGuid == cu.Guid && x.CompanyGuid == requestedCompanyGuid);
        if (!belongsToCompany)
        {
            _logger.LogWarning(
                "User {userGuid} tried to select company {companyGuid} without belonging to it",
                cu.Guid, requestedCompanyGuid);
            throw new Authorization403ForbiddenException("You do not belong to the selected company.");
        }

        return requestedCompanyGuid;
    }
}