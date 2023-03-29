﻿using ITBees.UserManager.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITBees.BaseServices.Platforms.Interfaces;
using ITBees.Interfaces.Repository;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Helpers;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace ITBees.UserManager.Services
{
    public class LoginService<T> : ILoginService<T> where T : IdentityUser
    {
        private readonly IUserManager _userManager;
        private readonly IReadOnlyRepository<UserAccount> _userReadOnlyRepository;
        private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyReadOnlyRepository;
        private readonly IWriteOnlyRepository<UserAccount> _userWriteOnlyRepository;
        private readonly IConfigurationRoot _configurationRoot;
        private readonly ICurrentDateTimeService _currentDateTimeService;

        public LoginService(
            IUserManager userManager,
            IReadOnlyRepository<UserAccount> userReadOnlyRepository,
            IReadOnlyRepository<UsersInCompany> usersInCompanyReadOnlyRepository,
            IConfigurationRoot configurationRoot, 
            IWriteOnlyRepository<UserAccount> userWriteOnlyRepository, 
            ICurrentDateTimeService currentDateTimeService)
        {
            _userManager = userManager;
            _userReadOnlyRepository = userReadOnlyRepository;
            _usersInCompanyReadOnlyRepository = usersInCompanyReadOnlyRepository;
            _configurationRoot = configurationRoot;
            _userWriteOnlyRepository = userWriteOnlyRepository;
            _currentDateTimeService = currentDateTimeService;
        }

        /// <summary>
        /// Use this method very carefully, it allows to get token without password so, You have to be sure that You know what are You doing.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<TokenVm> LoginAfterEmailConfirmation(string email)
        {
            var userAccount = GetUserAccount(email);
            return GetTokenVm(email, userAccount);
        }

        public async Task<TokenVm> Login(string email, string pass)
        {
            var userAccount = await GetUserIdAfterThePasswordCheck(email, pass);

            return GetTokenVm(email, userAccount);
        }

        private TokenVm GetTokenVm(string email, UserAccount userAccount)
        {
            var expiryInMinutes = Convert.ToInt32((string) _configurationRoot.GetSection("TokenLifeTime").Value);
            var tokenSecretKey = _configurationRoot.GetSection("TokenSecretKey").Value;
            var tokenIssuer = _configurationRoot.GetSection("TokenIssuer").Value;
            var tokenAudience = _configurationRoot.GetSection("TokenAudience").Value;

            Dictionary<string, string> claims = new Dictionary<string, string>()
            {
                {"LastUsedCompanyGuid", userAccount.LastUsedCompanyGuid?.ToString()},
                {"Language", userAccount.Language.Code},
                {"DisplayName", userAccount.DisplayName},
            };

            _userWriteOnlyRepository.UpdateData(u => u.Email == email, userAccountUpdate =>
            {
                userAccountUpdate.LastLoginDateTime = _currentDateTimeService.GetCurrentDate();
                userAccountUpdate.LoginsCount = userAccount.LoginsCount + 1;
            });
            var token = new JwtTokenBuilder()
                .AddSecurityKey(JwtSecurityKey.Create(tokenSecretKey))
                .AddSubject(userAccount.Guid.ToString())
                .AddIssuer(tokenIssuer)
                .AddAudience(tokenAudience)
                .AddExpiry(expiryInMinutes)
                .AddClaims(claims)
                .Build();

            var tokenVm = new TokenVm() {TokenExpirationDate = token.ValidTo, Value = token.Value};
            return tokenVm;
        }

        private async Task<UserAccount> GetUserIdAfterThePasswordCheck(string email, string pass)
        {
            UserAccount userAccount = GetUserAccount(email);
            
            var identityUser = await _userManager.FindByEmailAsync(email);

            if (!await _userManager.CheckPasswordAsync((T) identityUser, pass)) throw new UnauthorizedAccessException(Translate.Get(()=>Translations.UserManager.UserLogin.IncorrectEmailOrPassword, userAccount.Language));
            if (identityUser.EmailConfirmed) return userAccount;
            if (identityUser.EmailConfirmed == false) throw new Exception(Translate.Get(() => Translations.UserManager.UserLogin.EmailNotConfirmed, userAccount.Language));

            var tmpToken = await _userManager.GenerateEmailConfirmationTokenAsync((T) identityUser);

            await _userManager.ConfirmEmailAsync((T) identityUser, tmpToken);

            return userAccount;
        }

        private UserAccount GetUserAccount(string email)
        {
            UserAccount userAccount;
            try
            {
                userAccount = _userReadOnlyRepository.GetFirst(x => x.Email == email, x => x.Language);
                if (userAccount.LastUsedCompanyGuid == null)
                {
                    var usersInCompanies =
                        _usersInCompanyReadOnlyRepository.GetData(x => x.UserAccountGuid == userAccount.Guid);
                    userAccount.LastUsedCompanyGuid = usersInCompanies.First().CompanyGuid;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"{Translate.Get(() => Translations.UserManager.UserLogin.EmailNotConfirmed, new En())} { email} , {e.Message}");
            }

            return userAccount;
        }
    }
}