﻿using System.Threading.Tasks;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services;
using Microsoft.AspNetCore.Identity;

namespace ITBees.UserManager.Interfaces.Services
{
    public interface ILoginService<T> where T : IdentityUser
    {
        Task<TokenVm> Login(string email, string pass, string lang);
        Task<TokenVm> LoginAfterEmailConfirmation(string email, string lang);
        Task<ConfirmEmailResult> ConfirmEmail(string googlePayloadEmail);
    }
}