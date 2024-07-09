using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System;
using System.Linq;
using ITBees.Models.Languages;
using ITBees.UserManager.Interfaces.Services;

namespace ITBees.UserManager.Services
{
    public class AspCurrentUserService : IAspCurrentUserService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IReadOnlyRepository<UsersInCompany> _usersInCompanyRoRepository;

        public AspCurrentUserService(IHttpContextAccessor _contextAccessor, 
            IReadOnlyRepository<UsersInCompany> usersInCompanyRoRepository)
        {
            this._contextAccessor = _contextAccessor;
            _usersInCompanyRoRepository = usersInCompanyRoRepository;
        }
        public Guid? GetCurrentUserGuid()
        {
            var claimsIdentity = (_contextAccessor.HttpContext.User?.Identity as ClaimsIdentity);
            if (claimsIdentity.IsAuthenticated)
            {
                var claim = claimsIdentity.Claims.First();
                return new Guid(claim.Value);
            }
            else
            {
                return null;
            }
        }

        public CurrentUser GetCurrentUser()
        {
            var claimsIdentity = (_contextAccessor.HttpContext.User?.Identity as ClaimsIdentity);
            if (claimsIdentity.IsAuthenticated)
            {
                var claim = claimsIdentity.Claims.First();
                var LastUsedCompanyGuid = claimsIdentity.Claims.FirstOrDefault(x => x.Type == "LastUsedCompanyGuid").Value;
                var displayName = claimsIdentity.Claims.FirstOrDefault(x => x.Type == "DisplayName").Value;
                var email = claimsIdentity.Claims.FirstOrDefault(x => x.Type == "Email")!.Value;
                var language = claimsIdentity.Claims.FirstOrDefault(x => x.Type == "Language").Value;
                var userRoles = claimsIdentity.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x=>x.Value).ToArray();
                return new CurrentUser()
                {
                    Guid = new Guid(claim.Value), 
                    LastUsedCompanyGuid = new Guid(LastUsedCompanyGuid), 
                    Language = new InheritedMapper.DerivedAsTFromStringClassResolver<Language>().GetInstance(language),
                    DisplayName = displayName,
                    UserRoles = userRoles,
                    Email = email
                };
            }
            else
            {
                return null;
            }
        }

        public TypeOfOperation GetMyAcceessToCompany(Guid companyGuid)
        {
            var currentUserGuid = GetCurrentUserGuid();
            var userInCompany = _usersInCompanyRoRepository.GetData(x => x.UserAccountGuid == currentUserGuid && x.CompanyGuid == companyGuid).FirstOrDefault();
            if (userInCompany == null)
            {
                throw new AuthorizationException(AuthorizationExceptionMessages.You_dont_have_any_acceess_to_company);
            }

            return TypeOfOperation.RoRw;
        }

        public bool TryCanIDoForCompany(TypeOfOperation typeOfOperation, Guid companyGuid)
        {
            if (GetMyAcceessToCompany(companyGuid) == TypeOfOperation.RoRw)
            {
                return true;
            }

            throw new AuthorizationException(AuthorizationExceptionMessages.You_dont_have_acceess_enough_right_for_specified_company);
        }

        public bool CurrentUserIsPlatformOperator()
        {
            var currentUser = GetCurrentUser();
            if (currentUser != null && currentUser.UserRoles.Contains("PlatformOperator"))
            {
                return true;
            }

            return false;
        }
    }
}