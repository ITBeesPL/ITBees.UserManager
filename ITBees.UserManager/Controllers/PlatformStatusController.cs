using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

public class PlatformStatusController : RestfulControllerBase<PlatformStatusController>
{
    private readonly IPlatformStatusService _platformStatusService;

    public PlatformStatusController(ILogger<PlatformStatusController> logger, IPlatformStatusService platformStatusService) : base(logger)
    {
        _platformStatusService = platformStatusService;
    }
    [HttpGet]
    [Produces<PlatformStatusVm>]
    public IActionResult Get(string lang)
    {
        return ReturnOkResult(()=>_platformStatusService.GetCurrentStatus(lang));
    }

    [HttpPut]
    [Produces<PlatformStatusVm>]
    public IActionResult Put([FromBody] PlatformStatusUm platformStatusUm)
    {
        return ReturnOkResult(() => _platformStatusService.ChangePlatformStatus(platformStatusUm));
    }

    [HttpPost]
    [Produces<PlatformStatusVm>]
    public IActionResult Post([FromBody] PlatformStatusIm platformStatusIm)
    {
        return ReturnOkResult(() => _platformStatusService.CreateNewPlatformStatus(platformStatusIm));
    }
}