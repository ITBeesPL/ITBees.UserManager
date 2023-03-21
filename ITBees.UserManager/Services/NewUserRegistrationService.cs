using System;
using System.Linq;
using System.Threading.Tasks;
using InheritedMapper;
using ITBees.Interfaces.Repository;
using ITBees.Models.Companies;
using ITBees.Models.Languages;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ITBees.UserManager.Services
{
    public class NewUserRegistrationService<T> : INewUserRegistrationService where T : IdentityUser, new()
    {
        private readonly IUserManager _userManager;
        private readonly IWriteOnlyRepository<UserAccount> _userAccountWriteOnlyRepository;
        private readonly IWriteOnlyRepository<Company> _companyWoRepository;
        private readonly IReadOnlyRepository<Company> _companyRoRepository;
        private readonly IWriteOnlyRepository<UsersInCompany> _usersInCompanyWoRepo;
        private readonly ILogger<NewUserRegistrationService<T>> _logger;
        private readonly IAspCurrentUserService _aspCurrentUserService;

        public NewUserRegistrationService(IUserManager userManager,
            IWriteOnlyRepository<UserAccount> userAccountWriteOnlyRepository,
            IWriteOnlyRepository<Company> companyWoRepository,
            IReadOnlyRepository<Company> companyRoRepository,
            IWriteOnlyRepository<UsersInCompany> usersInCompanyWoRepo,
            ILogger<NewUserRegistrationService<T>> logger,
            IAspCurrentUserService aspCurrentUserService)
        {
            _userManager = userManager;
            _userAccountWriteOnlyRepository = userAccountWriteOnlyRepository;
            _companyWoRepository = companyWoRepository;
            _companyRoRepository = companyRoRepository;
            _usersInCompanyWoRepo = usersInCompanyWoRepo;
            _logger = logger;
            _aspCurrentUserService = aspCurrentUserService;
        }

        public async Task<NewUserRegistrationResult> RegisterNewUser(NewUserRegistrationIm newUserRegistrationIm)
        {
            var newUser = new T()
            {
                UserName = newUserRegistrationIm.Email,
                Email = newUserRegistrationIm.Email
            };

            var result = await _userManager.CreateAsync(newUser, newUserRegistrationIm.Password);
            var userLanguage = GetUserLanguage(newUserRegistrationIm);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "DuplicateUserName"))
                    throw new Exception(Translate.Get(() => Translations.UserManager.NewUserRegistration.EmailAlreadyRegistered, userLanguage));

                throw new Exception(Translate.Get(() => Translations.UserManager.NewUserRegistration.Errors.ErrorWhileRegisteringAUserAccount, userLanguage));
            }

            UserAccount userSavedData = null;
            var currentUserGuid = _aspCurrentUserService.GetCurrentUserGuid();
            
            if (currentUserGuid == null)
                currentUserGuid = new Guid((await _userManager.FindByEmailAsync(newUserRegistrationIm.Email)).Id);

            try
            {
                userSavedData = _userAccountWriteOnlyRepository.InsertData(
                    new UserAccount()
                    {
                        Email = newUserRegistrationIm.Email,
                        Guid = new Guid(newUser.Id),
                        Phone = newUserRegistrationIm.Phone,
                        FirstName = newUserRegistrationIm.FirstName,
                        LastName = newUserRegistrationIm.LastName,
                        LanguageId = userLanguage.Id
                    });

                if (newUserRegistrationIm.CompanyGuid == null)
                {
                    CreateCompanyAndAddCurrentUser(newUserRegistrationIm, newUser, currentUserGuid, 
                        userSavedData, userLanguage);
                }
                else
                {
                    CheckIfCurrentUserIsOwnerOfCompanyAndAddNewUserToThisCompany(newUserRegistrationIm,
                        currentUserGuid, userSavedData, userLanguage);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                var user = new UserAccount()
                {
                    Email = newUserRegistrationIm.Email,
                    Guid = new Guid(newUser.Id)
                };

                if (userSavedData == null)
                {
                    throw new Exception(e.Message);
                }

                return new NewUserRegistrationResult(userSavedData.Guid, e.Message);
            }

            return new NewUserRegistrationResult(userSavedData.Guid, string.Empty);
        }

        private Language GetUserLanguage(NewUserRegistrationIm newUserRegistrationIm)
        {
            Language userLanguage = null;
            userLanguage = newUserRegistrationIm.Language != null ? new DerivedAsTFromStringClassResolver<Language>().GetInstance(newUserRegistrationIm.Language) : new En();

            return userLanguage;
        }

        private void CheckIfCurrentUserIsOwnerOfCompanyAndAddNewUserToThisCompany(
            NewUserRegistrationIm newUserRegistrationIm, Guid? currentUserGuid, UserAccount userSavedData, Language userLanguage)
        {
            if (currentUserGuid.HasValue)
            {
                var company =
                    _companyRoRepository.GetData(x => x.Guid == newUserRegistrationIm.CompanyGuid).FirstOrDefault();

                if (company == null)
                    throw new Exception(Translate.Get(
                        () => Translations.UserManager.NewUserRegistration.CouldNotFindCompanyWithSpecifiedGuid,
                        userLanguage));

                if (company.OwnerGuid == currentUserGuid)
                {
                    _usersInCompanyWoRepo.InsertData(new UsersInCompany()
                    {
                        CompanyGuid = company.Guid,
                        AddedByGuid = currentUserGuid.Value,
                        AddedDate = DateTime.Now,
                        UserAccountGuid = userSavedData.Guid
                    });
                }
                else
                {
                    throw new Exception(Translate.Get(
                        () => Translations.UserManager.NewUserRegistration.ToAddNewUserYouMustBeCompanyOwner,
                        userLanguage));
                }
            }
            else
            {
                throw new Exception(Translate.Get(() => Translations.UserManager.NewUserRegistration.IfYouWantToAddNewUserToCompany, userLanguage));
            }
        }

        private void CreateCompanyAndAddCurrentUser(NewUserRegistrationIm newUserRegistrationIm, T newUser,
            Guid? currentUserGuid, UserAccount userSavedData, Language userLanguage)
        {
            if (string.IsNullOrEmpty(newUserRegistrationIm.CompanyName))
            {
                newUserRegistrationIm.CompanyName = Translate.Get(() => Translations.UserManager.NewUserRegistration.DefaultPrivateCompanyName, userLanguage);
            }

            var company = _companyWoRepository.InsertData(new Company()
            {
                CompanyName = newUserRegistrationIm.CompanyName,
                Created = DateTime.Now,
                CreatedByGuid = new Guid(newUser.Id),
                IsActive = true,
                OwnerGuid = new Guid(newUser.Id)
            });

            _usersInCompanyWoRepo.InsertData(new UsersInCompany()
            {
                CompanyGuid = company.Guid,
                AddedByGuid = currentUserGuid.Value,
                AddedDate = DateTime.Now,
                UserAccountGuid = userSavedData.Guid
            });
        }
    }
}