using System;
using System.Threading.Tasks;
using ITBees.UserManager.Controllers.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces;

public interface IAcceptAccountService<T> where T : IdentityUser<Guid>, new()
{
    Task<AcceptAccountResultVm> AcceptAccount(AcceptAccountIm acceptAccountIm);
}