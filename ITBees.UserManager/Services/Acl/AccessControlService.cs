using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Roles;
using ITBees.Models.Users;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services.Acl
{
    /// <summary>
    /// Who may act on a company through the UserManager (today: invite someone to it, POST /NewInvitedUserRegistration):
    /// the platform operator, the owner of the company or one of its members - never someone from outside it. What a
    /// member may give the invited person is limited separately (IPermissionService.CheckRoleGrant: no role holding
    /// more than the member holds there). An application with a stricter rule registers its own IAccessControlService
    /// after FasDependencyRegistration.Register.
    /// </summary>
    public class AccessControlService : IAccessControlService
    {
        private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
        private readonly IReadOnlyRepository<Company> _companyRoRepo;
        private readonly ILogger<AccessControlService> _logger;

        public AccessControlService(IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo,
            IReadOnlyRepository<Company> companyRoRepo,
            ILogger<AccessControlService> logger)
        {
            _usersInCompanyRoRepo = usersInCompanyRoRepo;
            _companyRoRepo = companyRoRepo;
            _logger = logger;
        }

        public void Setup(List<Type> types)
        {
        }

        public AccessControlResult CanDo(CurrentUser getCurrentUser, Type type, string methodName, Guid companyGuid)
        {
            if (getCurrentUser == null)
                return new AccessControlResult(false, "You must be logged in to do it.");

            if (getCurrentUser.UserRoles != null && getCurrentUser.UserRoles.Contains(Role.PlatformOperator))
                return new AccessControlResult(true, string.Empty);

            // Older companies have no OwnerGuid - their creator owns them (as in Kilometrówka).
            if (_usersInCompanyRoRepo.HasData(x => x.UserAccountGuid == getCurrentUser.Guid && x.CompanyGuid == companyGuid) ||
                _companyRoRepo.HasData(x => x.Guid == companyGuid &&
                                            (x.OwnerGuid == getCurrentUser.Guid ||
                                             (x.OwnerGuid == null && x.CreatedByGuid == getCurrentUser.Guid))))
                return new AccessControlResult(true, string.Empty);

            _logger.LogWarning("Access denied: user {UserGuid} is not a member of company {CompanyGuid}, {Type}.{Method}",
                getCurrentUser.Guid, companyGuid, type?.Name, methodName);

            return new AccessControlResult(false, "You don't have access to this company.");
        }
    }
}
