using System;
using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

[Authorize(Roles = Role.PlatformOperator)]
public class UserRolePermissionsController : RestfulControllerBase<UserRolePermissionsController>
{
    private readonly IUserRolePermissionsService _userRolePermissionsService;

    public UserRolePermissionsController(ILogger<UserRolePermissionsController> logger,
        IUserRolePermissionsService userRolePermissionsService) : base(logger)
    {
        _userRolePermissionsService = userRolePermissionsService;
    }

    [HttpGet]
    [Produces<UserRolePermissionsVm>]
    public IActionResult Get(Guid roleGuid)
    {
        return ReturnOkResult(() => _userRolePermissionsService.Get(roleGuid));
    }

    [HttpPut]
    [Produces<UserRolePermissionsVm>]
    public IActionResult Put([FromBody] UserRolePermissionsUm userRolePermissionsUm)
    {
        return ReturnOkResult(() => _userRolePermissionsService.Update(userRolePermissionsUm));
    }
}
