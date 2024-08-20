using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Services.Passwords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [Authorize]
    public class ChangePasswordController : RestfulControllerBase<ChangePasswordController>
    {
        private readonly IChangePasswordService _changePasswordService;

        public ChangePasswordController(IChangePasswordService changePasswordService,
            ILogger<ChangePasswordController> logger) : base(logger)
        {
            _changePasswordService = changePasswordService;
        }

        [Produces(typeof(ChangePassResultVm))]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChangePasswordIm changePasswordIm)
        {
            return await ReturnOkResultAsync(async () => await _changePasswordService.ChangePassword(changePasswordIm));
        }
    }
}