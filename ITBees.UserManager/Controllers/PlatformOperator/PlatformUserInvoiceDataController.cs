using System;
using System.Collections.Generic;
using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.PlatformOperator.Models;
using ITBees.UserManager.Services.PlatformOperator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers.PlatformOperator;

/// <summary>
/// Dane do faktury firm wskazanego użytkownika (użytkownik może należeć do wielu firm) -
/// zakładka "Dane do faktury" w szczegółach użytkownika panelu administracyjnego.
/// </summary>
[Authorize(Roles = Role.PlatformOperator)]
public class PlatformUserInvoiceDataController : RestfulControllerBase<PlatformUserInvoiceDataController>
{
    private readonly IPlatformUserInvoiceDataService _platformUserInvoiceDataService;

    public PlatformUserInvoiceDataController(ILogger<PlatformUserInvoiceDataController> logger,
        IPlatformUserInvoiceDataService platformUserInvoiceDataService) : base(logger)
    {
        _platformUserInvoiceDataService = platformUserInvoiceDataService;
    }

    [HttpGet]
    [Produces<List<PlatformUserCompanyInvoiceDataVm>>]
    public IActionResult Get(Guid userGuid)
    {
        return ReturnOkResult(() => _platformUserInvoiceDataService.GetForUser(userGuid));
    }

    [HttpPut]
    [Produces<PlatformUserCompanyInvoiceDataVm>]
    public IActionResult Put([FromBody] PlatformUserInvoiceDataUm platformUserInvoiceDataUm)
    {
        return ReturnOkResult(() => _platformUserInvoiceDataService.Update(platformUserInvoiceDataUm));
    }
}
