using System.Collections.Generic;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers
{
    [Authorize]
    public class AwaitingInvitationsController : RestfulControllerBase<AwaitingInvitationsController>
    {
        private readonly IAwaitingInvitationsService _awaitingInvitationsService;

        public AwaitingInvitationsController(ILogger<AwaitingInvitationsController> logger,
            IAwaitingInvitationsService awaitingInvitationsService) : base(logger)
        {
            _awaitingInvitationsService = awaitingInvitationsService;
        }

        [HttpGet]
        [Produces(typeof(List<AwaitingInvitationVm>))]
        public IActionResult Get()
        {
            List<AwaitingInvitationVm> result = _awaitingInvitationsService.GetMyInvitations();
            return Ok(result);
        }
    }
}