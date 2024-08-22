using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.RestfulApiControllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

public class ResendActivationLinkController : RestfulControllerBase<ResendActivationLinkController>
{
    private readonly INewUserRegistrationService _newUserRegistrationService;

    public ResendActivationLinkController(
        ILogger<ResendActivationLinkController> logger,
        INewUserRegistrationService newUserRegistrationService) : base(logger)
    {
        _newUserRegistrationService = newUserRegistrationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FasApiErrorVm), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public Task<IActionResult> Post(string email)
    {
        return ReturnOkResultAsync(async()=>await _newUserRegistrationService.ResendConfirmationEmail(email));
    }
}