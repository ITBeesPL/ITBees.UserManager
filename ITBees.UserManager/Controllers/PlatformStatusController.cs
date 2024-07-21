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
    public IActionResult Get(string lang)
    {
        return ReturnOkResult(()=>_platformStatusService.GetCurrentStatus(lang));
    }

    [HttpPut]
    public IActionResult Put([FromBody] PlatformStatusUm platformStatusUm)
    {
        return ReturnOkResult(() => _platformStatusService.ChangePlatformStatus(platformStatusUm));
    }

    [HttpPost]
    public IActionResult Post([FromBody] PlatformStatusIm platformStatusIm)
    {
        return ReturnOkResult(() => _platformStatusService.CreateNewPlatformStatus(platformStatusIm));
    }
}