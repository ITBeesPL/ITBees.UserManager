using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces;

namespace ITBees.UserManager.Services
{
    public class MyCompaniesService : IMyCompaniesService
    {
        private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
        private readonly IAspCurrentUserService _aspCurrentUserService;

        public MyCompaniesService(IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo, IAspCurrentUserService aspCurrentUserService)
        {
            _usersInCompanyRoRepo = usersInCompanyRoRepo;
            _aspCurrentUserService = aspCurrentUserService;
        }

        public IEnumerable<Company> GetMyCompaniesQueryable()
        {
            var cu =_aspCurrentUserService.GetCurrentUser();
            var companies = _usersInCompanyRoRepo.GetDataQueryable(x => x.UserAccountGuid == cu.Guid).Select(x=>x.Company);
            return companies;
        }
    }
}