using System;
using ITBees.Models.Companies;
using ITBees.Models.Languages;

namespace ITBees.UserManager.Interfaces
{
    public interface ICompanyBootstrapService
    {
        /// <summary>
        /// Ensures the given user account has at least one company assigned (via UsersInCompany).
        /// If the user already has a company, returns null. Otherwise creates a default private
        /// company, sets LastUsedCompanyGuid on the user account, and inserts UsersInCompany.
        /// </summary>
        Company EnsureUserHasCompany(Guid userAccountGuid, string desiredCompanyName, Language userLanguage);
    }
}
