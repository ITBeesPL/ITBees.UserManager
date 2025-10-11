using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITBees.BaseServices.Platforms.Interfaces;
using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Interfaces.Repository;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using ITBees.Translations;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.Helpers;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services
{
    public class LoginService<T> : ILoginService<T> where T : IdentityUser<Guid>
    {
        private readonly IUserManager<T> _userManager;
        private readonly IReadOnlyRepository<UserAccount> _userReadOnlyRepository;
        private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyReadOnlyRepository;
        private readonly IWriteOnlyRepository<UserAccount> _userWriteOnlyRepository;
        private readonly IConfigurationRoot _configurationRoot;
        private readonly ICurrentDateTimeService _currentDateTimeService;
        private readonly ILogger<LoginService<T>> _logger;

        public LoginService(
            IUserManager<T> userManager,
            IReadOnlyRepository<UserAccount> userReadOnlyRepository,
            IReadOnlyRepository<UsersInCompany> usersInCompanyReadOnlyRepository,
            IConfigurationRoot configurationRoot,
            IWriteOnlyRepository<UserAccount> userWriteOnlyRepository,
            ICurrentDateTimeService currentDateTimeService,
            ILogger<LoginService<T>> logger)
        {
            _userManager = userManager;
            _userReadOnlyRepository = userReadOnlyRepository;
            _usersInCompanyReadOnlyRepository = usersInCompanyReadOnlyRepository;
            _configurationRoot = configurationRoot;
            _userWriteOnlyRepository = userWriteOnlyRepository;
            _currentDateTimeService = currentDateTimeService;
            _logger = logger;
        }

        /// <summary>
        /// Use this method very carefully, it allows to get token without password so, You have to be sure that You know what are You doing.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<TokenVm> LoginAfterEmailConfirmation(string email, string lang)
        {
            var userAccount = GetUserAccount(email, lang);
            return await GetTokenVm(email, userAccount);
        }

        /// <summary>
        /// Use with caution, it will confirm any specified email account
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<ConfirmEmailResult> ConfirmEmail(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                await _userManager.ConfirmEmailAsync(user, token);
                return new ConfirmEmailResult() { Success = true };
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error during email confirmation for email {email} message : {e.Message}");
                return new ConfirmEmailResult() { Success = false, Message = e.Message };
            }
        }

        public async Task<MyAccountWithTokenVm> GetMyAccountWithTokenWithoutAuthorization(MyAccountVm myAccount, string language)
        {
            var token = await this.LoginAfterEmailConfirmation(myAccount.Email, language);
            return new MyAccountWithTokenVm(myAccount, token);
        }

        public virtual async Task<TokenVm> Login(string email, string pass, string lang)
        {
            var userAccount = await GetUserIdAfterThePasswordCheck(email, pass, lang);

            return await GetTokenVm(email, userAccount);
        }

        private async Task<TokenVm> GetTokenVm(string email, UserAccount userAccount)
        {
            var expiryInMinutes = Convert.ToInt32((string)_configurationRoot.GetSection("TokenLifeTime").Value);
            var tokenSecretKey = _configurationRoot.GetSection("TokenSecretKey").Value;
            var tokenIssuer = _configurationRoot.GetSection("TokenIssuer").Value;
            var tokenAudience = _configurationRoot.GetSection("TokenAudience").Value;

            var owinUser = await _userManager.FindByEmailAsync(email);
            var roles = await _userManager.GetRolesAsync(owinUser);

            Dictionary<string, string> claims = new Dictionary<string, string>()
            {
                {"LastUsedCompanyGuid", userAccount.LastUsedCompanyGuid?.ToString()},
                {"Language", userAccount.Language.Code},
                {"DisplayName", userAccount.DisplayName},
                {"Email", userAccount.Email},
                {"UserAccountModules", FormatUserAccountModulesForJwtToken(userAccount)},
                {"RoleInCompanies", FormatCompanyRolesForJwtToken(userAccount)},
                {"Roles" ,FormatRolesForJwtToken(roles)}
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
                .Build(roles);

            var tokenVm = new TokenVm() { TokenExpirationDate = token.ValidTo, Value = token.Value };
            return tokenVm;
        }

        private string FormatRolesForJwtToken(IList<string> roles)
        {
            return string.Join(";", roles);
        }

        private string FormatUserAccountModulesForJwtToken(UserAccount userAccount)
        {
            var formatUserAccountModulesForJwtToken = string.Join(";",
                userAccount.UserAccountModules?.Select(x =>
                    $"{x.ModuleType}-{x.ModuleName}-{x.MethodName}-{x.AllowedTypeOfOperation}"));
            return formatUserAccountModulesForJwtToken;
        }

        private string FormatCompanyRolesForJwtToken(UserAccount userAccount)
        {
            var formatCompanyRolesForJwtToken = string.Join(";", userAccount.UsersInCompanies.Select(x => $"CompanyGuid:{x.CompanyGuid}|CompanyName:{x.Company?.CompanyName}|RoleGuid:{x.IdentityRole?.Id}|RoleName:{x.IdentityRole?.Name}"));
            return formatCompanyRolesForJwtToken;
        }

        private async Task<UserAccount> GetUserIdAfterThePasswordCheck(string email, string pass, string lang)
        {
            UserAccount userAccount = GetUserAccount(email, lang);

            var identityUser = await _userManager.FindByEmailAsync(email);

            if (!await _userManager.CheckPasswordAsync(identityUser, pass)) throw new UnauthorizedAccessException(Translate.Get(() => Translations.UserManager.UserLogin.IncorrectEmailOrPassword, userAccount.Language));
            if (identityUser.EmailConfirmed) return userAccount;
            if (identityUser.EmailConfirmed == false)
                throw new Authorization403ForbiddenException
                    (Translate.Get(() => Translations.UserManager.UserLogin.EmailNotConfirmed, userAccount.Language));

            var tmpToken = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);

            await _userManager.ConfirmEmailAsync(identityUser, tmpToken);

            return userAccount;
        }

        private UserAccount GetUserAccount(string email, string lang)
        {
            UserAccount userAccount;
            
            userAccount = _userReadOnlyRepository.GetDataQueryable(x => x.Email == email)
                .Include(x => x.UsersInCompanies)
                .Include(x => x.UserAccountModules)
                .Include(x => x.Language).Include(x => x.UsersInCompanies)
                .ThenInclude(x => x.IdentityRole).FirstOrDefault();

            if (userAccount == null)
            {
                _logger.LogError("Could not find user account for email {email}", email);
                throw new ArgumentException($"{Translate.Get(() => Translations.UserManager.UserLogin.EmailNotRegistered, lang)} {email}");
            }

            try
            {
                if (userAccount.LastUsedCompanyGuid == null || userAccount.LastUsedCompanyGuid == new Guid("00000000-0000-0000-0000-000000000000"))
                {
                    var usersInCompanies =
                        _usersInCompanyReadOnlyRepository.GetData(x => x.UserAccountGuid == userAccount.Guid);
                    if (usersInCompanies.Count == 0)
                    {
                        throw new Exception(
                            $"Could not find any users in company company, last used company guid is null, user guid : {userAccount.Guid}");
                    }

                    userAccount.LastUsedCompanyGuid = usersInCompanies.First().CompanyGuid;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error during getting last used company guid for email {email}, message : {message}", email, e.Message);
                throw new Exception($"{Translate.Get(() => Translations.UserManager.UserLogin.EmailNotConfirmed, new En())} {email} , {e.Message}");
            }

            return userAccount;
        }
    }
}