using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NewUserRegistrationController : RestfulControllerBase<NewUserRegistrationController>
    {
        private readonly INewUserRegistrationService _newUserRegistrationService;
        private readonly ILogger<NewUserRegistrationController> _logger;

        public NewUserRegistrationController(INewUserRegistrationService newUserRegistrationService,
            ILogger<NewUserRegistrationController> logger) : base(logger)
        {
            _newUserRegistrationService = newUserRegistrationService;
            _logger = logger;
        }

        [Produces(typeof(NewUserRegistrationVm))]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NewUserRegistrationIm newUserIm)
        {
            return await ReturnOkResultAsync(async () =>
            {
                var result = await _newUserRegistrationService.CreateNewUser(newUserIm);

                return new NewUserRegistrationVm
                {
                    UserGuid = result.UserGuid,
                    ErrorMessages = result.ErrorMessages
                };
            });
        }
    }
}