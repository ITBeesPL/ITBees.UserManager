using System.Collections.Generic;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

[Authorize]
public class UserRolesController : RestfulControllerBase<UserRolesController>
{
    private readonly IUserRolesService _userRolesService;

    public UserRolesController(ILogger<UserRolesController> logger, IUserRolesService userRolesService) : base(logger)
    {
        _userRolesService = userRolesService;
    }

    [HttpGet]
    [Produces<List<UserRoleVm>>]
    public IActionResult Get()
    {
        return ReturnOkResult(() => _userRolesService.Get());
    }
}