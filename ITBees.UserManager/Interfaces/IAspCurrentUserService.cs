using System;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces
{
    public interface IAspCurrentUserService
    {
        Guid? GetCurrentUserGuid();
        CurrentUser GetCurrentUser();
        CurrentSessionUser GetCurrentSessionUser();
        TypeOfOperation GetMyAcceessToCompany(Guid companyGuid);
        /// <summary>
        /// Check If I'm allowed to do expected kind of operation ie Read or write for specified company. It throws AuthorizationException if You don't have any access to specified company
        /// </summary>
        /// <param name="typeOfOperation"></param>
        /// <param name="companyGuid"></param>
        /// <returns></returns>
        bool TryCanIDoForCompany(TypeOfOperation typeOfOperation, Guid companyGuid);

        bool CurrentUserIsPlatformOperator();
        bool CurrentUserIsInRole(string role);
    }
}