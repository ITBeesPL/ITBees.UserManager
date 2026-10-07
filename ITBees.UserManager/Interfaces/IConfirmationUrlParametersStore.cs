using System;
using System.Collections.Generic;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces
{
    public interface IConfirmationUrlParametersStore
    {
        void Save(Guid userAccountGuid, List<ConfirmationUrlParameterIm> confirmationUrlParameters);
        List<ConfirmationUrlParameterIm> Get(Guid userAccountGuid);

        /// <summary>
        /// Removes the stored parameters of the account - the registration rollback calls it before it deletes
        /// the account, which the stored row references. A default body keeps other implementations compiling.
        /// </summary>
        void Delete(Guid userAccountGuid)
        {
        }
    }
}
