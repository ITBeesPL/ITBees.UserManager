using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.GenericControllersAttributes;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [CustomControllerName("ConfirmRegistration")]
    [GenericRestControllerNameConvention]
    [Route("/ConfirmRegistration")]
    public class ConfirmRegistrationController<T> : RestfulControllerBase<ConfirmRegistrationController<T>> where T : IdentityUser
    {
        private readonly IConfirmRegistrationService<T> _confirmRegistrationService;

        public ConfirmRegistrationController(IConfirmRegistrationService<T> confirmRegistrationService, ILogger<ConfirmRegistrationController<T>> logger) : base(logger)
        {
            _confirmRegistrationService = confirmRegistrationService;
        }

        [HttpPost]
        [Produces(typeof(TokenVm))]
        public async Task<IActionResult> Post([FromBody] ConfirmRegistrationIm confirmRegistrationIm)
        {
            return await ReturnOkResultAsync(async () => await _confirmRegistrationService.ConfirmRegistrationEmailAndGetSessinToken(confirmRegistrationIm));
        }
    }
}