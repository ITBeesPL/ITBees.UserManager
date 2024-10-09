using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class NewInvitedUserRegistrationController : RestfulControllerBase<NewInvitedUserRegistrationController>
    {
        private readonly INewUserRegistrationService _newUserRegistrationService;
        private readonly ILogger<NewInvitedUserRegistrationController> _logger;

        public NewInvitedUserRegistrationController(INewUserRegistrationService newUserRegistrationService,
            ILogger<NewInvitedUserRegistrationController> logger) : base(logger)
        {
            _newUserRegistrationService = newUserRegistrationService;
            _logger = logger;
        }

        [Produces(typeof(NewUserRegistrationVm))]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NewUserRegistrationWithInvitationIm newUserIm)
        {

            return await ReturnOkResultAsync(async () =>
                {
                    var result = await _newUserRegistrationService.CreateAndInviteNewUserToCompany(newUserIm);
                    return new NewUserRegistrationVm()
                    {
                        UserGuid = result.UserGuid,
                        ErrorMessages = result.ErrorMessages

                    };
                }
            );
        }
    }
}