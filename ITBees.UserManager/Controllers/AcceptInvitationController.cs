using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    public class AcceptInvitationController : RestfulControllerBase<AcceptInvitationController>
    {
        private readonly IAcceptInvitationService _acceptInvitationService;

        public AcceptInvitationController(ILogger<AcceptInvitationController> logger,
            IAcceptInvitationService acceptInvitationService) : base(logger)
        {
            _acceptInvitationService = acceptInvitationService;
        }

        [HttpGet]
        [Produces(typeof(AcceptInvitationResultVm))]
        public IActionResult Get(bool emailInvitation, string email, string company)
        {
            var acceptResult = _acceptInvitationService.Accept(emailInvitation, email, company);
            return Ok(acceptResult);
        }
    }
}