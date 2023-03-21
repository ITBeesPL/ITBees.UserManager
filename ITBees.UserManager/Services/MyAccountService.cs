using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Services;

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
        public ITBees.Models.MyAccount.MyAccount GetMyAccountData()
        {
            var currentUserGuid = _aspCurrentUserService.GetCurrentUser();
            var usersInCompany = _usersInCompanyRepository
                .GetData(x => x.UserAccountGuid == currentUserGuid.Guid, 
                    x => x.Company, 
                    x => x.UserAccount).ToList();
            return new ITBees.Models.MyAccount.MyAccount()
            {
                Guid = usersInCompany.First().UserAccount.Guid,
                Email = usersInCompany.First().UserAccount.Email,
                Phone = usersInCompany.First().UserAccount.Phone,
                FirstName = usersInCompany.First().UserAccount.FirstName,
                LastName = usersInCompany.First().UserAccount.LastName,
                Companies = usersInCompany.Select(x=>x.Company).ToList(),
                LastUsedCompanyGuid = currentUserGuid.LastUsedCompanyGuid,
                Language = currentUserGuid.Language
            };
        }
    }
}