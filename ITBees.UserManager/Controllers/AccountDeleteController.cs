using System;
using System.Threading.Tasks;
using ITBees.RestfulApiControllers;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Controllers;

public class AccountDeleteController<T> : RestfulControllerBase<AccountDeleteController<T>> where T : IdentityUser<Guid> 
{
    private readonly IAccountDeleteService<T> _accountDeleteService;

    public AccountDeleteController(ILogger<AccountDeleteController<T>> logger,
        IAccountDeleteService<T> accountDeleteService) : base(logger)
    {
        _accountDeleteService = accountDeleteService;
    }


    /// <summary>
    /// Use this endpoint to delete own account, or specify guid or email (use null for guid if want to use email field) of another user for delete.
    /// </summary>
    /// <param name="accountGuid">If provided, then account of other user will be deleted</param>
    /// <returns></returns>
    [HttpDelete]
    [Produces<DeleteResultVm>]
    public async Task<IActionResult> Del(Guid? accountGuid = null, string authKey = null, string email = null)
    {
        return await ReturnOkResultAsync(async () => await _accountDeleteService.Delete(accountGuid, authKey, email));
    }
}