using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

[Authorize(Roles = Role.PlatformOperator)]
public class PermissionCatalogController : RestfulControllerBase<PermissionCatalogController>
{
    private readonly IPermissionCatalogService _permissionCatalogService;

    public PermissionCatalogController(ILogger<PermissionCatalogController> logger,
        IPermissionCatalogService permissionCatalogService) : base(logger)
    {
        _permissionCatalogService = permissionCatalogService;
    }

    [HttpGet]
    [Produces<PermissionCatalogVm>]
    public IActionResult Get()
    {
        return ReturnOkResult(() => _permissionCatalogService.Get());
    }
}
