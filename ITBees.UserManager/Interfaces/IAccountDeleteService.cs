using System;
using System.Threading.Tasks;

namespace ITBees.UserManager.Interfaces;

public interface IAccountDeleteService
{
    Task Delete(Guid? accountGuid);
}