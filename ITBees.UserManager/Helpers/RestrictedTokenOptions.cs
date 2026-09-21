using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ITBees.UserManager.Helpers
{
    /// <summary>
    /// Describes the tokens an application issues with narrowed-down rights - a kiosk session, a device
    /// token and the like - next to the regular login tokens of the platform. The platform recognises
    /// them by a claim and never exchanges them for a login token (see PUT /MyAccount), because that
    /// would hand the full account to whoever holds the narrowed-down token.
    /// <para>
    /// By convention such a token carries the <see cref="TokenPurposeClaimType"/> claim; a regular login
    /// token must not. An application that marks its tokens differently adds its own claim types:
    /// <c>services.Configure&lt;RestrictedTokenOptions&gt;(o =&gt; o.ClaimTypes.Add("MyClaim"));</c>
    /// </para>
    /// </summary>
    public class RestrictedTokenOptions
    {
        public const string TokenPurposeClaimType = "TokenPurpose";

        /// <summary>Claim types whose presence marks a token as restricted.</summary>
        public ISet<string> ClaimTypes { get; } =
            new HashSet<string>(StringComparer.Ordinal) { TokenPurposeClaimType };

        public bool IsRestricted(ClaimsPrincipal principal)
        {
            return principal != null && principal.Claims.Any(claim => ClaimTypes.Contains(claim.Type));
        }
    }
}
