﻿using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Logging;
using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.RestfulApiControllers.Models;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services;

public class UserRolesService : IUserRolesService
{
    private readonly IReadOnlyRepository<IdentityRole> _identityRoleRoRepo;
    private readonly IWriteOnlyRepository<IdentityRole> _identityRoleRwRepo;
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly ILogger<UserRolesService> _logger;

    public UserRolesService(IReadOnlyRepository<IdentityRole> identityRoleRoRepo,
        IWriteOnlyRepository<IdentityRole> identityRoleRwRepo,
        IAspCurrentUserService aspCurrentUserService,
        ILogger<UserRolesService> logger)
    {
        _identityRoleRoRepo = identityRoleRoRepo;
        _identityRoleRwRepo = identityRoleRwRepo;
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
            if (_identityRoleRoRepo.HasData(x => x.Name == roleName))
                throw new FasApiErrorException(new FasApiErrorVm("Role already exists", 400, ""));

            var result = _identityRoleRwRepo.InsertData(new IdentityRole(roleName));
            return new UserRoleVm(result);
        }
        _logger.LogCritical($"User role : {roleName} was created by {_aspCurrentUserService.GetCurrentUser().DisplayName}");

        throw new UnauthorizedAccessException("You are not platform operator to add new roles.");
    }

    public UserRoleVm GetRole(Guid roleGuid)
    {
        return _identityRoleRoRepo.GetData(x => x.Id == roleGuid.ToString()).Select(x => new UserRoleVm(x)).FirstOrDefault();
    }

    public void Delete(Guid roleGuid)
    {
        var allRoles = _identityRoleRoRepo.GetData(x => true).ToList();

        var roleToDelete = allRoles.FirstOrDefault(x => x.Id == roleGuid.ToString());
        if (roleToDelete == null)
            throw new FasApiErrorException(new FasApiErrorVm("Role not exists", 400, ""));
        if (roleToDelete.Name == "PlatformOperator")
            throw new FasApiErrorException(new FasApiErrorVm("Role Platform operator could not be deleted", 400, ""));

        _logger.LogCritical($"User role : {roleToDelete.Name} was deleted by {_aspCurrentUserService.GetCurrentUser().DisplayName}");

        _identityRoleRwRepo.DeleteData(x => x.Id == roleToDelete.Id);
    }
}