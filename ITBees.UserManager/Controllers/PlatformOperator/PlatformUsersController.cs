using ITBees.Interfaces.Repository;
using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Services.PlatformOperator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers.PlatformOperator;

[Authorize(Roles = Role.PlatformOperator)]
public class PlatformUsersController : RestfulControllerBase<PlatformUsersController>
{
    private readonly IPlatformUsersService _platformUsersService;

    public PlatformUsersController(ILogger<PlatformUsersController> logger, IPlatformUsersService platformUsersService) : base(logger)
    {
        _platformUsersService = platformUsersService;
    }

    [HttpGet]
    public IActionResult Get(string? search, int? page, int? pageSize, string? sortColumn, SortOrder? sortOrder)
    {
        return ReturnOkResult(()=>_platformUsersService.GetUsers(search, page, pageSize, sortColumn, sortOrder));
    }
}