using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces;
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
    public Task<IActionResult> Post(string email)
    {
        return ReturnOkResultAsync(async()=>await _newUserRegistrationService.ResendConfirmationEmail(email));
    }
}