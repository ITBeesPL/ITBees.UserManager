using System;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Registration
{
    public class CompanyBootstrapService<TCompany> : ICompanyBootstrapService
        where TCompany : Company, new()
    {
        private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
        private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;
        private readonly IWriteOnlyRepository<TCompany> _companyWoRepo;
        private readonly IWriteOnlyRepository<UserAccount> _userAccountWoRepo;
        private readonly ILogger<CompanyBootstrapService<TCompany>> _logger;

        public CompanyBootstrapService(
            IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
            IWriteOnlyRepository<TCompany> companyWoRepo,
            IWriteOnlyRepository<UserAccount> userAccountWoRepo,
            ILogger<CompanyBootstrapService<TCompany>> logger)
        {
            _usersInCompanyRoRepo = usersInCompanyRoRepo;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
            _companyWoRepo = companyWoRepo;
            _userAccountWoRepo = userAccountWoRepo;
            _logger = logger;
        }

        public Company EnsureUserHasCompany(Guid userAccountGuid, string desiredCompanyName, Language userLanguage)
        {
            var existing = _usersInCompanyRoRepo.GetData(x => x.UserAccountGuid == userAccountGuid);
            if (existing.Count > 0)
            {
                return null;
            }

            var companyName = desiredCompanyName;
            if (string.IsNullOrEmpty(companyName))
            {
                try
                {
                    companyName = Translate.Get(
                        () => Translations.UserManager.NewUserRegistration.DefaultPrivateCompanyName,
                        userLanguage ?? new En());
                }
                catch (Exception translateEx)
                {
                    _logger.LogWarning(translateEx,
                        "Translate.Get failed for DefaultPrivateCompanyName - using hardcoded fallback.");
                }

                if (string.IsNullOrEmpty(companyName))
                {
                    companyName = "Private";
                }
            }

            var company = _companyWoRepo.InsertData(new TCompany()
            {
                CompanyName = companyName,
                Created = DateTime.Now,
                CreatedByGuid = userAccountGuid,
                IsActive = true,
                OwnerGuid = userAccountGuid
            });

            _userAccountWoRepo.UpdateData(x => x.Guid == userAccountGuid,
                x => { x.LastUsedCompanyGuid = company.Guid; });

            _usersInCompanyWoRepo.InsertData(new UsersInCompany()
            {
                CompanyGuid = company.Guid,
                AddedByGuid = userAccountGuid,
                AddedDate = DateTime.Now,
                UserAccountGuid = userAccountGuid,
            });

            _logger.LogWarning(
                "Self-healed missing company for user {userAccountGuid} with default name '{companyName}' (company guid {companyGuid}).",
                userAccountGuid, companyName, company.Guid);

            return company;
        }
    }
}
