using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.DbModels;

namespace ITBees.UserManager.Interfaces
{
    public interface IAwaitingInvitationsService
    {
        List<AwaitingInvitationVm> GetMyInvitations();
    }

    public class AwaitingInvitationsService : IAwaitingInvitationsService
    {
        private readonly IAspCurrentUserService _aspCurrentUserService;
        private readonly IReadOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesRoRepo;

        public AwaitingInvitationsService(IAspCurrentUserService aspCurrentUserService, IReadOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesRoRepo)
        {
            _aspCurrentUserService = aspCurrentUserService;
            _usersInvitationsToCompaniesRoRepo = usersInvitationsToCompaniesRoRepo;
        }
        public List<AwaitingInvitationVm> GetMyInvitations()
        {
            var currentUser = _aspCurrentUserService.GetCurrentUser();

            return _usersInvitationsToCompaniesRoRepo
                .GetData(x => x.Applied == false && x.UserAccountGuid == currentUser.Guid, x => x.Company, x => x.CreatedBy)
                .Select(x => new AwaitingInvitationVm(x)).ToList();
        }
    }
}