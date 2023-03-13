using System;
using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NewUserRegistrationController : ControllerBase
    {
        private readonly INewUserRegistrationService _newUserRegistrationService;
        private readonly ILogger<NewUserRegistrationController> _logger;

        public NewUserRegistrationController(INewUserRegistrationService newUserRegistrationService,
            ILogger<NewUserRegistrationController> logger)
        {
            _newUserRegistrationService = newUserRegistrationService;
            _logger = logger;
        }
        
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NewUserRegistrationIm newUserIm)
        {
            try
            {
                var result = await _newUserRegistrationService.RegisterNewUser(newUserIm);
                    
                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }
        }
    }
}