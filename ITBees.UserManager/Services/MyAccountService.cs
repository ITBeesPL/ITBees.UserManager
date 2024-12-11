using System;
using System.Linq;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using Nelibur.ObjectMapper;

namespace ITBees.UserManager.Services
{
    public class MyAccountService : IMyAccountServie
    {
        private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRepository;
        private readonly IAspCurrentUserService _aspCurrentUserService;

        public MyAccountService(IReadOnlyRepository<UsersInCompany> usersInCompanyRepository,
            IAspCurrentUserService aspCurrentUserService)
        {
            _usersInCompanyRepository = usersInCompanyRepository;
            _aspCurrentUserService = aspCurrentUserService;
        }

        public MyAccountVm GetMyAccountData()
        {
            var currentUserGuid = _aspCurrentUserService.GetCurrentUser();

            var usersInCompany = _usersInCompanyRepository
                .GetData(x => x.UserAccountGuid == currentUserGuid.Guid,
                    x => x.Company,
                    x => x.UserAccount,
                    x => x.UserAccount.Language,
                    x => x.IdentityRole);

            var lastUsedCompany =
                usersInCompany.FirstOrDefault(x => x.CompanyGuid == currentUserGuid.LastUsedCompanyGuid);

            string displayName = string.IsNullOrEmpty(usersInCompany.First().UserAccount.FirstName)
                ? usersInCompany.First().UserAccount.Email
                : $"{usersInCompany.First().UserAccount.FirstName} {usersInCompany.First().UserAccount.LastName}";
            
            var myAccount = new Models.MyAccount.MyAccount()
            {
                Guid = usersInCompany.First().UserAccount.Guid,
                Email = usersInCompany.First().UserAccount.Email,
                Phone = usersInCompany.First().UserAccount.Phone,
                FirstName = usersInCompany.First().UserAccount.FirstName,
                LastName = usersInCompany.First().UserAccount.LastName,
                Companies = usersInCompany.Select(x => new CompanyWithUserRole(x.Company, x.IdentityRole?.Name, x.IdentityRole?.Id)).ToList(),
                LastUsedCompanyGuid = currentUserGuid.LastUsedCompanyGuid,
                LastUsedCompany = new CompanyWithUserRole(lastUsedCompany.Company, lastUsedCompany.IdentityRole?.Name, lastUsedCompany.IdentityRole?.Id),
                Language = usersInCompany.First().UserAccount.Language,
                DisplayName = displayName
            };

            return new MyAccountVm(myAccount);
        }
    }
}