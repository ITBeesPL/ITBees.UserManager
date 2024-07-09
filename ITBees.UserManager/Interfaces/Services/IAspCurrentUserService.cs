using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface IAspCurrentUserService
    {
        Guid? GetCurrentUserGuid();
        CurrentUser GetCurrentUser();
        TypeOfOperation GetMyAcceessToCompany(Guid companyGuid);
        /// <summary>
        /// Check If I'm allowed to do expected kind of operation ie Read or write for specified company. It throws AuthorizationException if You don't have any access to specified company
        /// </summary>
        /// <param name="typeOfOperation"></param>
        /// <param name="companyGuid"></param>
        /// <returns></returns>
        bool TryCanIDoForCompany(TypeOfOperation typeOfOperation, Guid companyGuid);

        bool CurrentUserIsPlatformOperator();
    }
}