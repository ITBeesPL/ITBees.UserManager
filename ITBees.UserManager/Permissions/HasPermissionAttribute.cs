using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.RestfulApiControllers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ITBees.UserManager.Permissions;

/// <summary>
/// Declares which permission an endpoint requires (any of the given keys). Checked twice:
/// 1. before the action runs - the user must hold the permission in at least one of their companies;
/// 2. by every company access check made while the request runs (IAspCurrentUserService.TryCanIDoForCompany)
///    - the user must hold the permission in exactly that company.
/// An action-level attribute replaces the controller-level one. A denial is answered 403 and logged.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class HasPermissionAttribute : Attribute, IAuthorizationFilter
{
    public HasPermissionAttribute(params string[] permissionKeys)
    {
        // Without a key the attribute would let everyone in while looking like a guard.
        if (permissionKeys == null || permissionKeys.Length == 0)
            throw new ArgumentException("At least one permission key is required.", nameof(permissionKeys));

        PermissionKeys = permissionKeys;
    }

    public IReadOnlyCollection<string> PermissionKeys { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var mostSpecificAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<HasPermissionAttribute>()
            .LastOrDefault();
        if (ReferenceEquals(mostSpecificAttribute, this) == false)
            return;

        // Anonymous requests are left to [Authorize].
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            return;

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        if (permissionService.CanEnterEndpoint(PermissionKeys))
            return;

        context.Result = new ObjectResult(new FasApiErrorVm("You don't have permission to perform this operation.",
            StatusCodes.Status403Forbidden, ""))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
