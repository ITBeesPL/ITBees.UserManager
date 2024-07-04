﻿using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

[Authorize]
public class AccountDeleteController : RestfulControllerBase<AccountDeleteController>
{
    private readonly IAccountDeleteService _accountDeleteService;

    public AccountDeleteController(ILogger<AccountDeleteController> logger,
        IAccountDeleteService accountDeleteService) : base(logger)
    {
        _accountDeleteService = accountDeleteService;
    }


    /// <summary>
    /// Use this endpoint to delete own account, or specify guid of another user for delete.
    /// </summary>
    /// <param name="accountGuid">If provided, then account of other user will be deleted</param>
    /// <returns></returns>
    [HttpDelete]
    public async Task<IActionResult> Del(Guid? accountGuid = null)
    {
        try
        {
            await _accountDeleteService.Delete(accountGuid);
            return Ok();
        }
        catch (Exception e)
        {
            return CreateBaseErrorResponse(e, accountGuid);
        }
    }
}