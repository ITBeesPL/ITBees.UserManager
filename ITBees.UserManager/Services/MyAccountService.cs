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

        public MyAccountService(IReadOnlyRepository<UsersInCompany> usersInCompanyRepository, IAspCurrentUserService aspCurrentUserService)
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
                    x => x.UserAccount, x => x.UserAccount.Language).ToList();
            string displayName = string.IsNullOrEmpty(usersInCompany.First().UserAccount.FirstName) ? usersInCompany.First().UserAccount.Email : $"{usersInCompany.First().UserAccount.FirstName} {usersInCompany.First().UserAccount.LastName}";
            var myAccount = new ITBees.Models.MyAccount.MyAccount()
            {
                Guid = usersInCompany.First().UserAccount.Guid,
                Email = usersInCompany.First().UserAccount.Email,
                Phone = usersInCompany.First().UserAccount.Phone,
                FirstName = usersInCompany.First().UserAccount.FirstName,
                LastName = usersInCompany.First().UserAccount.LastName,
                Companies = usersInCompany.Select(x => x.Company).ToList(),
                LastUsedCompanyGuid = currentUserGuid.LastUsedCompanyGuid,
                LastUsedCompany = new Company()
                {
                    CompanyName = usersInCompany.First().Company.CompanyName,
                    Created = usersInCompany.First().Company.Created,
                    CreatedByGuid = usersInCompany.First().Company.CreatedByGuid,
                    Guid = usersInCompany.First().Company.Guid,
                    IsActive = usersInCompany.First().Company.IsActive
                },
                Language = usersInCompany.First().UserAccount.Language,
                DisplayName = displayName
            };

            return TinyMapper.Map<MyAccountVm>(myAccount);
        }
    }
}