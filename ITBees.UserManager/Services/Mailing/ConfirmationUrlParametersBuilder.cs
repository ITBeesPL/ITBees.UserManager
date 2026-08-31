using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Services.Mailing
{
    public static class ConfirmationUrlParametersBuilder
    {
        private static readonly HashSet<string> ReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "token",
            "email",
            "tokenAuth",
            "company",
            "companyGuid",
            "issuedAt",
            "emailInvitation",
            "key"
        };

        public static string Build(IEnumerable<ConfirmationUrlParameterIm> confirmationUrlParameters)
        {
            if (confirmationUrlParameters == null)
            {
                return string.Empty;
            }

            var alreadyAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new StringBuilder();

            foreach (var parameter in confirmationUrlParameters)
            {
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Name))
                {
                    continue;
                }

                var name = parameter.Name.Trim();
                if (ReservedNames.Contains(name) || alreadyAdded.Add(name) == false)
                {
                    continue;
                }

                result.Append("&")
                    .Append(HttpUtility.UrlEncode(name))
                    .Append("=")
                    .Append(HttpUtility.UrlEncode(parameter.Value ?? string.Empty));
            }

            return result.ToString();
        }
    }
}
