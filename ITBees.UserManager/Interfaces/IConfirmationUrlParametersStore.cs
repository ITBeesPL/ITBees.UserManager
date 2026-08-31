using System;
using System.Collections.Generic;
using ITBees.UserManager.Interfaces.Models;

namespace ITBees.UserManager.Interfaces
{
    public interface IConfirmationUrlParametersStore
    {
        void Save(Guid userAccountGuid, List<ConfirmationUrlParameterIm> confirmationUrlParameters);
        List<ConfirmationUrlParameterIm> Get(Guid userAccountGuid);
    }
}
