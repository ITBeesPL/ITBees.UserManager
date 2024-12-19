using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Invitations.Models;
using ITBees.UserManager.Services.Invitations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers.Invitations;

public class InvitationExistingUserController : RestfulControllerBase<InvitationExistingUserController>
{
    private readonly IUserInvitationService _iUserInvitationService;

    public InvitationExistingUserController(ILogger<InvitationExistingUserController> logger,
        IUserInvitationService iUserInvitationService) : base(logger)
    {
        _iUserInvitationService = iUserInvitationService;
    }

    [HttpPost]
    [Produces<ApplyInvitationResultForExistingUserVm>]
    public IActionResult Post([FromBody] InvitationExistingUserIm invitationExistingUserIm)
    {
        return ReturnOkResult(() => { _iUserInvitationService.ApplyExistingUser(invitationExistingUserIm); });
    }
}