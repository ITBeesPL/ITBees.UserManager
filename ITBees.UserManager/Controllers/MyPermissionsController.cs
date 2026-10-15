using System;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

[Authorize]
public class MyPermissionsController : RestfulControllerBase<MyPermissionsController>
{
    private readonly IMyPermissionsService _myPermissionsService;

    public MyPermissionsController(ILogger<MyPermissionsController> logger, IMyPermissionsService myPermissionsService)
        : base(logger)
    {
        _myPermissionsService = myPermissionsService;
    }

    [HttpGet]
    [Produces<MyPermissionsVm>]
    public IActionResult Get(Guid? companyGuid)
    {
        return ReturnOkResult(() => _myPermissionsService.Get(companyGuid));
    }
}
