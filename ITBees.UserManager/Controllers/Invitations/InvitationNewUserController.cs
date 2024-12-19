using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Invitations.Models;
using ITBees.UserManager.Services.Invitations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers.Invitations;

public class InvitationNewUserController : RestfulControllerBase<InvitationNewUserController>
{
    private readonly IUserInvitationService _iUserInvitationService;

    public InvitationNewUserController(ILogger<InvitationNewUserController> logger,
        IUserInvitationService iUserInvitationService) : base(logger)
    {
        _iUserInvitationService = iUserInvitationService;
    }

    [HttpPost]
    [Produces<ApplyInvitationResultForNewUserVm>]
    public IActionResult Post([FromBody] InvitationNewUserIm invitationNewUserIm)
    {
        return ReturnOkResult(() => { _iUserInvitationService.ApplyNewUser(invitationNewUserIm); });
    }
}