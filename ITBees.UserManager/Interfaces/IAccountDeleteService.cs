using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces;

public interface IAccountDeleteService<T> where T : IdentityUser<Guid>
{
    Task Delete(Guid? accountGuid, string authKey = null);
}