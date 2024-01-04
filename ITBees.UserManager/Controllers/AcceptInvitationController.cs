using System;
using ITBees.Interfaces.Platforms;
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
        private readonly IPlatformSettingsService _platformSettingsService;

        public AcceptInvitationController(ILogger<AcceptInvitationController> logger,
            IAcceptInvitationService acceptInvitationService, 
            IPlatformSettingsService platformSettingsService) : base(logger)
        {
            _acceptInvitationService = acceptInvitationService;
            _platformSettingsService = platformSettingsService;
        }

        [HttpGet]
        [Produces(typeof(AcceptInvitationResultVm))]
        public IActionResult Get(bool emailInvitation, string email, Guid companyGuid)
        {
            var acceptResult = _acceptInvitationService.Accept(emailInvitation, email, companyGuid);
            return Redirect(_platformSettingsService.GetSetting("ApplicationSiteUrl"));
        }
    }
}