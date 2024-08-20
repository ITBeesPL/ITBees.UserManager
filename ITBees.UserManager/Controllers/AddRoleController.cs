using System.Threading.Tasks;
using ITBees.Models.Languages;
using ITBees.Models.Roles;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{

    [Authorize(Roles = Role.PlatformOperator)]
    public class AddRoleController : RestfulControllerBase<AddRoleController>
    {
        private readonly IRoleAddingService _roleAddingService;

        public AddRoleController(ILogger<AddRoleController> logger, IRoleAddingService roleAddingService) : base(logger)
        {
            _roleAddingService = roleAddingService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AddRoleIm addRoleIm)
        {
            return await ReturnOkResultAsync(async () => await _roleAddingService.AddRole(addRoleIm.Email, addRoleIm.Role, new Pl()));
        }
    }
}