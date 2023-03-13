using ITBees.RestfulApiControllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [Authorize]
    public class AddUserToCompanyController : RestfulControllerBase<AddUserToCompanyController>
    {
        public AddUserToCompanyController(ILogger<AddUserToCompanyController> logger) : base(logger)
        {
        }
    }
}