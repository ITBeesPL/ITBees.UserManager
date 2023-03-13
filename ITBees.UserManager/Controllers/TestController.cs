using ITBees.UserManager.Interfaces.Services;
using ITBees.UserManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    public class TestController : RestfulApiControllers.RestfulControllerBase<TestController>
    {
        public TestController(IUserManager fas) : base(null)
        {
            
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("test");
        }
    }
}