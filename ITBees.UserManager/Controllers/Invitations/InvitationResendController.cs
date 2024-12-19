using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers.Invitations;

[Authorize]
public class InvitationResendController : RestfulControllerBase<InvitationResendController>
{
    private readonly IInvitationResendService _invitationResendService;

    public InvitationResendController(ILogger<InvitationResendController> logger,
        IInvitationResendService invitationResendService) : base(logger)
    {
        _invitationResendService = invitationResendService;
    }

    public IActionResult Post([FromBody] InvitationResendIm invitationResendIm)
    {
        return ReturnOkResult(() => { _invitationResendService.Resend(invitationResendIm);});
    }
}