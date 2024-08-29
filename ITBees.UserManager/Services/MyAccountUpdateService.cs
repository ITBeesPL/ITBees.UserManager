using System;
using System.Linq;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Interfaces.Repository;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services;

public class MyAccountUpdateService : IMyAccountUpdateService
{
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly IWriteOnlyRepository<UserAccount> _userAccountRwRepo;
    private readonly ILogger<MyAccountUpdateService> _logger;
    private readonly IReadOnlyRepository<Language> _langRoRepo;

    public MyAccountUpdateService(IAspCurrentUserService aspCurrentUserService,
        IWriteOnlyRepository<UserAccount> userAccountRwRepo,
        ILogger<MyAccountUpdateService> logger,
        IReadOnlyRepository<Language> langRoRepo)
    {
        _aspCurrentUserService = aspCurrentUserService;
        _userAccountRwRepo = userAccountRwRepo;
        _logger = logger;
        _langRoRepo = langRoRepo;
    }


    public void UpdateMyAccount(MyAccountIm myAccountIm)
    {
        try
        {
            var cu = _aspCurrentUserService.GetCurrentUser();
            if (cu == null)
            {
                throw new UnauthorizedAccessException("You must be logged in!");
            }
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
                    x.LastUsedCompanyGuid = myAccountIm.LastUsedCompanyGuid;
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
                    x.LastUsedCompanyGuid = myAccountIm.LastUsedCompanyGuid;
                }, x=>x.Language, x=>x.UserAccountModules);
                if (userAccount.Count == 0)
                {
                    throw new Exception("Didn't update my account language, record not found");
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            throw;
        }
    }
}