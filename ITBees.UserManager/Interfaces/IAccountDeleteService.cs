using System;

namespace ITBees.UserManager.Interfaces;

public interface IAccountDeleteService
{
    void Delete(Guid? accountGuid);
}