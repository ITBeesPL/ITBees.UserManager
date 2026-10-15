using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.RestfulApiControllers.Models;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services;

public class UserRolesService : IUserRolesService
{
    private readonly IReadOnlyRepository<FasIdentityRole> _identityRoleRoRepo;
    private readonly IWriteOnlyRepository<FasIdentityRole> _identityRoleRwRepo;
    private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepo;
    private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;
    private readonly IReadOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesRoRepo;
    private readonly IWriteOnlyRepository<UsersInvitationsToCompanies> _usersInvitationsToCompaniesWoRepo;
    private readonly IReadOnlyRepository<IdentityUserRole<Guid>> _identityUserRoleRoRepo;
    private readonly IWriteOnlyRepository<IdentityUserRole<Guid>> _identityUserRoleWoRepo;
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly ILogger<UserRolesService> _logger;

    public UserRolesService(IReadOnlyRepository<FasIdentityRole> identityRoleRoRepo,
        IWriteOnlyRepository<FasIdentityRole> identityRoleRwRepo,
        IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepo,
        IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
        IReadOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesRoRepo,
        IWriteOnlyRepository<UsersInvitationsToCompanies> usersInvitationsToCompaniesWoRepo,
        IReadOnlyRepository<IdentityUserRole<Guid>> identityUserRoleRoRepo,
        IWriteOnlyRepository<IdentityUserRole<Guid>> identityUserRoleWoRepo,
        IAspCurrentUserService aspCurrentUserService,
        ILogger<UserRolesService> logger)
    {
        _identityRoleRoRepo = identityRoleRoRepo;
        _identityRoleRwRepo = identityRoleRwRepo;
        _usersInCompanyRoRepo = usersInCompanyRoRepo;
        _usersInCompanyWoRepo = usersInCompanyWoRepo;
        _usersInvitationsToCompaniesRoRepo = usersInvitationsToCompaniesRoRepo;
        _usersInvitationsToCompaniesWoRepo = usersInvitationsToCompaniesWoRepo;
        _identityUserRoleRoRepo = identityUserRoleRoRepo;
        _identityUserRoleWoRepo = identityUserRoleWoRepo;
        _aspCurrentUserService = aspCurrentUserService;
        _logger = logger;
    }
    public List<UserRoleVm> Get()
    {
        if (_aspCurrentUserService.CurrentUserIsPlatformOperator())
        {
            return _identityRoleRoRepo.GetData(x => true).Select(x => new UserRoleVm(x)).ToList();
        }
        else
        {
            return _identityRoleRoRepo.GetData(x => true && x.Name != "PlatformOperator").Select(x => new UserRoleVm(x)).ToList();
        }
    }

    public UserRoleVm Create(string roleName)
    {
        if (roleName == "PlatformOperator" && _aspCurrentUserService.CurrentUserIsPlatformOperator() == false)
        {
            throw new FasApiErrorException(
                new FasApiErrorVm("This role is restricted only for platform operator accounts", 403, ""));
        }

        if (_aspCurrentUserService.CurrentUserIsPlatformOperator())
        {
            roleName = roleName?.Trim();
            if (string.IsNullOrEmpty(roleName))
                throw new FasApiErrorException(new FasApiErrorVm("Role name is required", 400, ""));

            // Identity looks roles up by NormalizedName (unique index); without it the role is invisible to
            // RoleManager/AddToRoleAsync and the same name could be added again.
            var normalizedName = roleName.ToUpperInvariant();
            if (_identityRoleRoRepo.HasData(x => x.NormalizedName == normalizedName || x.Name.ToUpper() == normalizedName))
                throw new FasApiErrorException(new FasApiErrorVm("Role already exists", 400, ""));

            var result = _identityRoleRwRepo.InsertData(new FasIdentityRole(roleName)
            {
                NormalizedName = normalizedName,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            });
            return new UserRoleVm(result);
        }
        _logger.LogCritical($"User role : {roleName} was created by {_aspCurrentUserService.GetCurrentUser().DisplayName}");

        throw new UnauthorizedAccessException("You are not platform operator to add new roles.");
    }

    public UserRoleVm GetRole(Guid roleGuid)
    {
        var role = _identityRoleRoRepo.GetFirst(x => x.Id == roleGuid);
        if (role == null)
            return null;

        return new UserRoleVm(role) { AssignedUsersCount = GetAssignedUserGuids(role.Id).Count };
    }

    public void Delete(Guid roleGuid, Guid? replacementRoleGuid)
    {
        if (_aspCurrentUserService.CurrentUserIsPlatformOperator() == false)
            throw new Authorization403ForbiddenException("Only a platform operator can delete roles.");

        var allRoles = _identityRoleRoRepo.GetData(x => true).ToList();

        var roleToDelete = allRoles.FirstOrDefault(x => x.Id == roleGuid);
        if (roleToDelete == null)
            throw new FasApiErrorException(new FasApiErrorVm("Role not exists", 400, ""));
        if (roleToDelete.Name == "PlatformOperator")
            throw new FasApiErrorException(new FasApiErrorVm("Role Platform operator could not be deleted", 400, ""));

        // Users holding the role must not be left with a dangling role: they move to the replacement role.
        var assignedUserGuids = GetAssignedUserGuids(roleToDelete.Id);
        FasIdentityRole replacementRole = null;
        if (assignedUserGuids.Count > 0)
        {
            if (replacementRoleGuid == null)
                throw new FasApiErrorException(new FasApiErrorVm(
                    $"The role is assigned to {assignedUserGuids.Count} users - choose a role to give them instead.",
                    400, ""));

            replacementRole = allRoles.FirstOrDefault(x => x.Id == replacementRoleGuid);
            if (replacementRole == null || replacementRole.Id == roleToDelete.Id)
                throw new FasApiErrorException(new FasApiErrorVm("Replacement role not found", 400, ""));
            // Deleting a role must never become a way of handing out platform-wide rights.
            if (string.Equals(replacementRole.Name, "PlatformOperator", StringComparison.OrdinalIgnoreCase))
                throw new FasApiErrorException(new FasApiErrorVm(
                    "The platform operator role cannot be given as a replacement", 400, ""));

            MoveAssignments(roleToDelete.Id, replacementRole.Id);
        }

        _logger.LogCritical("User role {RoleName} was deleted by {UserName}; {Count} users moved to {ReplacementRoleName}",
            roleToDelete.Name, _aspCurrentUserService.GetCurrentUser()?.DisplayName, assignedUserGuids.Count,
            replacementRole?.Name ?? "-");

        _identityRoleRwRepo.DeleteData(x => x.Id == roleToDelete.Id);
    }

    /// <summary>Users holding the role in a company, in a pending invitation or as a global Identity role.</summary>
    private HashSet<Guid> GetAssignedUserGuids(Guid roleId)
    {
        var userGuids = new HashSet<Guid>(_usersInCompanyRoRepo
            .GetData(x => x.IdentityRoleId == roleId)
            .Select(x => x.UserAccountGuid));
        userGuids.UnionWith(_usersInvitationsToCompaniesRoRepo
            .GetData(x => x.IdentityRoleId == roleId)
            .Select(x => x.UserAccountGuid));
        userGuids.UnionWith(_identityUserRoleRoRepo
            .GetData(x => x.RoleId == roleId)
            .Select(x => x.UserId));
        return userGuids;
    }

    private void MoveAssignments(Guid roleId, Guid replacementRoleId)
    {
        _usersInCompanyWoRepo.UpdateData(x => x.IdentityRoleId == roleId, x => x.IdentityRoleId = replacementRoleId);
        _usersInvitationsToCompaniesWoRepo.UpdateData(x => x.IdentityRoleId == roleId,
            x => x.IdentityRoleId = replacementRoleId);

        // Global roles: (UserId, RoleId) is the key, so the replacement is added only where it is missing.
        var globalHolders = _identityUserRoleRoRepo.GetData(x => x.RoleId == roleId).Select(x => x.UserId).ToList();
        if (globalHolders.Count == 0)
            return;

        var alreadyHolding = new HashSet<Guid>(_identityUserRoleRoRepo
            .GetData(x => x.RoleId == replacementRoleId && globalHolders.Contains(x.UserId))
            .Select(x => x.UserId));
        var missing = globalHolders
            .Where(x => alreadyHolding.Contains(x) == false)
            .Select(x => new IdentityUserRole<Guid> { UserId = x, RoleId = replacementRoleId })
            .ToList();
        if (missing.Count > 0)
            _identityUserRoleWoRepo.InsertData(missing);
        _identityUserRoleWoRepo.DeleteData(x => x.RoleId == roleId);
    }
}