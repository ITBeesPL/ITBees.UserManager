using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    public class CheckEmailAvailabilityAndConfirmationStatusController : RestfulControllerBase<CheckEmailAvailabilityAndConfirmationStatusController>
    {
        private readonly IEmailAvailabilityAndConfirmationStatusCheckingService _emailAvailabilityService;

        public CheckEmailAvailabilityAndConfirmationStatusController(
            IEmailAvailabilityAndConfirmationStatusCheckingService emailAvailabilityService,
            ILogger<CheckEmailAvailabilityAndConfirmationStatusController> logger) : base(logger)
        {
            _emailAvailabilityService = emailAvailabilityService;
        }

        [HttpGet]
        [Produces(typeof(CheckEmailStatusVm))]
        public async Task<IActionResult> Get(string email, string language)
        {
            var result = await _emailAvailabilityService.Check(email, language);
            return Ok(result);
        }
    }
}