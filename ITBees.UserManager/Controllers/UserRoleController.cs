using System;
using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

[Authorize(Roles = Role.PlatformOperator)]
public class UserRoleController : RestfulControllerBase<UserRoleController>
{
    private readonly IUserRolesService _userRolesService;

    public UserRoleController(ILogger<UserRoleController> logger, IUserRolesService userRolesService) : base(logger)
    {
        _userRolesService = userRolesService;
    }

    [HttpGet]
    [Produces<UserRoleVm>]
    public IActionResult Get(Guid roleGuid)
    {
        return ReturnOkResult(() => _userRolesService.GetRole(roleGuid));
    }

    [HttpPost]
    [Produces<UserRoleVm>]
    public IActionResult Post(string roleName)
    {
        return ReturnOkResult(() => _userRolesService.Create(roleName));
    }

    [HttpDelete]
    public IActionResult Del(Guid roleGuid)
    {
        return ReturnOkResult(() => _userRolesService.Delete(roleGuid));
    }
}