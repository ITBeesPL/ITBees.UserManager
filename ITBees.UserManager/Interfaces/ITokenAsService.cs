using System;
using System.Threading.Tasks;
using ITBees.UserManager.Controllers.PlatformOperator.Models;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces;

public interface ITokenAsService<T> where T : IdentityUser<Guid> 
{
    Task<TokenVm> GetToken(TokenAsIm tokenAsIm);
}